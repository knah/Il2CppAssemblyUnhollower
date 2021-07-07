using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using AssemblyUnhollower.Extensions;
using AssemblyUnhollower.Passes;
using Mono.Cecil;
using UnhollowerRuntimeLib.XrefScans;

namespace AssemblyUnhollower.Contexts
{
    public class MethodRewriteContext
    {
        public readonly TypeRewriteContext DeclaringType;
        public readonly MethodDefinition OriginalMethod;
        public readonly MethodDefinition NewMethod;

        public readonly bool OriginalNameObfuscated;

        public readonly long FileOffset;
        public readonly long Rva;

        public long MetadataInitFlagRva;
        public long MetadataInitTokenRva;

        public string UnmangledName { get; private set; }
        public string UnmangledNameWithSignature { get; private set; }
        
        public TypeDefinition? GenericInstantiationsStore { get; private set; }
        public TypeReference? GenericInstantiationsStoreSelfSubstRef { get; private set; }
        public TypeReference? GenericInstantiationsStoreSelfSubstMethodRef { get; private set; }
        public FieldReference NonGenericMethodInfoPointerField { get; private set; }

        public readonly List<XrefInstance> XrefScanResults = new List<XrefInstance>();

        public MethodRewriteContext(TypeRewriteContext declaringType, MethodDefinition originalMethod)
        {
            DeclaringType = declaringType;
            OriginalMethod = originalMethod;

            var passthroughNames = declaringType.AssemblyContext.GlobalContext.Options.PassthroughNames;

            OriginalNameObfuscated = !passthroughNames &&
                                     (OriginalMethod?.Name?.IsObfuscated(declaringType.AssemblyContext.GlobalContext
                                         .Options) ?? false);

            var reusingSystemInterface = declaringType.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemInterface;
            
            var newMethod = reusingSystemInterface ? declaringType.NewType.Methods.Single(it => it.Name == originalMethod.Name) : new MethodDefinition("", AdjustAttributes(originalMethod.Attributes), declaringType.AssemblyContext.Imports.Void);
            NewMethod = newMethod;

            if (reusingSystemInterface) return;

            if (originalMethod.CustomAttributes.Any(x => x.AttributeType.FullName == typeof(ExtensionAttribute).FullName))
            {
                newMethod.CustomAttributes.Add(new CustomAttribute(declaringType.AssemblyContext.Imports.ExtensionAttributeCtor));
            }

            if (originalMethod.HasGenericParameters)
            {
                var genericParams = originalMethod.GenericParameters;

                foreach (var oldParameter in genericParams)
                {
                    var genericParameter = new GenericParameter(oldParameter.Name, newMethod);
                    genericParameter.Attributes = oldParameter.Attributes.StripValueTypeConstraint();
                    newMethod.GenericParameters.Add(genericParameter);
                }
            }

            if (!Pass15GenerateMemberContexts.HasObfuscatedMethods && !passthroughNames && originalMethod.Name.IsObfuscated(declaringType.AssemblyContext.GlobalContext.Options))
                Pass15GenerateMemberContexts.HasObfuscatedMethods = true;

            FileOffset = originalMethod.ExtractOffset();
            Rva = originalMethod.ExtractRva();
            if (FileOffset != 0)
                declaringType.AssemblyContext.GlobalContext.MethodStartAddresses.Add(FileOffset);
        }

        public void CtorPhase2()
        {
            UnmangledName = UnmangleMethodName();
            UnmangledNameWithSignature = UnmangleMethodNameWithSignature();
            
            if (DeclaringType.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemInterface) return;

            NewMethod.Name = UnmangledName;
            NewMethod.ReturnType = DeclaringType.AssemblyContext.RewriteTypeRef(OriginalMethod.ReturnType);
            
            if (OriginalNameObfuscated)
                NewMethod.CustomAttributes.Add(
                    new CustomAttribute(DeclaringType.AssemblyContext.Imports.ObfuscatedNameAttributeCtor)
                    {
                        ConstructorArguments = {new CustomAttributeArgument(DeclaringType.AssemblyContext.Imports.String, OriginalMethod.Name)}
                    });

            if (DeclaringType.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.Default)
            {
                var nonGenericMethodInfoPointerField = new FieldDefinition(
                    "NativeMethodInfoPtr_" + UnmangledNameWithSignature,
                    FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly,
                    DeclaringType.AssemblyContext.Imports.IntPtr);
                DeclaringType.NewType.Fields.Add(nonGenericMethodInfoPointerField);

                NonGenericMethodInfoPointerField = new FieldReference(nonGenericMethodInfoPointerField.Name,
                    nonGenericMethodInfoPointerField.FieldType, DeclaringType.SelfSubstitutedRef);


                if (OriginalMethod.HasGenericParameters)
                {
                    var genericParams = OriginalMethod.GenericParameters;
                    var genericMethodInfoStoreType = new TypeDefinition("",
                        "MethodInfoStoreGeneric_" + UnmangledNameWithSignature + "`" + genericParams.Count,
                        TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                        DeclaringType.AssemblyContext.Imports.Object);
                    genericMethodInfoStoreType.DeclaringType = DeclaringType.NewType;
                    DeclaringType.NewType.NestedTypes.Add(genericMethodInfoStoreType);
                    GenericInstantiationsStore = genericMethodInfoStoreType;

                    var selfSubstRef = new GenericInstanceType(genericMethodInfoStoreType);
                    var selfSubstMethodRef = new GenericInstanceType(genericMethodInfoStoreType);

                    for (var index = 0; index < genericParams.Count; index++)
                    {
                        var oldParameter = genericParams[index];
                        var genericParameter = new GenericParameter(oldParameter.Name, genericMethodInfoStoreType);
                        genericMethodInfoStoreType.GenericParameters.Add(genericParameter);
                        selfSubstRef.GenericArguments.Add(genericParameter);
                        var newParameter = NewMethod.GenericParameters[index];
                        selfSubstMethodRef.GenericArguments.Add(newParameter);
                    }

                    var pointerField = new FieldDefinition("Pointer", FieldAttributes.Assembly | FieldAttributes.Static,
                        DeclaringType.AssemblyContext.Imports.IntPtr);
                    genericMethodInfoStoreType.Fields.Add(pointerField);

                    GenericInstantiationsStoreSelfSubstRef = DeclaringType.NewType.Module.ImportReference(selfSubstRef);
                    GenericInstantiationsStoreSelfSubstMethodRef =
                        DeclaringType.NewType.Module.ImportReference(selfSubstMethodRef);
                }
            }

            if (OriginalMethod.HasGenericParameters)
            {
                var genericParams = OriginalMethod.GenericParameters;

                for (var index = 0; index < genericParams.Count; index++)
                {
                    var oldParameter = genericParams[index];
                    var newParameter = NewMethod.GenericParameters[index];

                    foreach (var oldConstraint in oldParameter.Constraints)
                    {
                        if (oldConstraint.ConstraintType.FullName == "System.ValueType" ||
                            oldConstraint.ConstraintType.Resolve()?.IsInterface == true) continue;

                        newParameter.Constraints.Add(new GenericParameterConstraint(
                            DeclaringType.AssemblyContext.RewriteTypeRef(oldConstraint.ConstraintType)));
                    }
                }
            }

            DeclaringType.NewType.Methods.Add(NewMethod);
        }

        public void AssignExplicitOverrides()
        {
            // todo: translate explicit overrides based on names perhaps?; dumper doesn't generate those though, so interface implementations can end up scuffed
            // todo: properly rewrite the entire method signature including parameters and generic types in them
            foreach (var originalMethodOverride in OriginalMethod.Overrides)
            {
                var specificOverride =
                    DeclaringType.AssemblyContext.NewAssembly.MainModule.ImportReference(
                        DeclaringType.AssemblyContext.RewriteMethodRef(originalMethodOverride));
                
                NewMethod.Overrides.Add(specificOverride);
            }

            if (OriginalMethod.Overrides.Count == 0 && OriginalMethod.Name.Contains("."))
            {
                var originalImplementingInterface = OriginalMethod.DeclaringType.Interfaces.FirstOrDefault(it => CanMethodBeImplementingInterface(OriginalMethod.Name, it.InterfaceType));
                if (originalImplementingInterface != null)
                {
                    var interfaceRewrite = DeclaringType.AssemblyContext.RewriteTypeRef(originalImplementingInterface.InterfaceType);
                    var newImplementingInterface = DeclaringType.NewType.Interfaces.Single(it => it.InterfaceType.FullName == interfaceRewrite.FullName);
                    
                    var originalImplementedMethod = FindMatchingMethod(originalImplementingInterface.InterfaceType, OriginalMethod);

                    if (originalImplementedMethod != null)
                    {
                        var newMethod = FindMatchingMethod(newImplementingInterface.InterfaceType, originalImplementedMethod, false);
                        NewMethod.Overrides.Add(DeclaringType.AssemblyContext.NewAssembly.MainModule.ImportReference(newMethod));
                    }
                }
            }
        }

        private static MethodReference? FindMatchingMethod(TypeReference type, MethodReference originalMethod, bool typePrefixedName = true)
        {
            if (type is GenericInstanceType generic)
            {
                var baseReference = FindMatchingMethod(generic.ElementType, originalMethod,typePrefixedName);
                if (baseReference == null) return null;

                var newReference = new MethodReference(baseReference.Name, baseReference.ReturnType, type)
                {
                    HasThis = baseReference.HasThis,
                };
                
                foreach (var baseReferenceParameter in baseReference.Parameters)
                    newReference.Parameters.Add(baseReferenceParameter);
                return newReference;
            }

            return type.Resolve().Methods
                .Single(it =>
                    (typePrefixedName ? originalMethod.Name.EndsWith("." + it.Name) : originalMethod.Name == it.Name) && // todo: also match parameter types?
                    originalMethod.Parameters.Count == it.Parameters.Count);
        }

        private static bool CanMethodBeImplementingInterface(string methodName, TypeReference interfaceType)
        {
            if (interfaceType is GenericInstanceType generic)
            {
                var includedType = generic.ElementType.FullName;
                var backtickIndex = includedType.IndexOf('`');
                if (backtickIndex != -1)
                    includedType = includedType.Substring(0, backtickIndex);

                if (methodName.Length <= includedType.Length || !methodName.StartsWith(includedType))
                    return false;

                var nextChar = methodName[includedType.Length];
                return nextChar is '`' or '<' or '[' or '.';
            }

            return methodName.StartsWith(interfaceType.FullName + ".");
        }

        private MethodAttributes AdjustAttributes(MethodAttributes original)
        {
            original &= ~(MethodAttributes.MemberAccessMask); // todo: handle Object overload correctly
            original &= ~(MethodAttributes.PInvokeImpl);
            original &= ~(MethodAttributes.CheckAccessOnOverride);
            original |= MethodAttributes.Public;
            return original;
        }

        private string UnmangleMethodName()
        {
            var method = OriginalMethod;
            
            if (method.Name == "GetType" && method.Parameters.Count == 0)
                return "GetIl2CppType";
            
            if (DeclaringType.AssemblyContext.GlobalContext.Options.PassthroughNames)
                return method.Name;
            
            if (method.Name == ".ctor")
                return ".ctor";
            
            if(method.Name.IsObfuscated(DeclaringType.AssemblyContext.GlobalContext.Options))
                return UnmangleMethodNameWithSignature();

            if (method.Name.IsInvalidInSource())
                return method.Name.FilterInvalidInSourceChars();

            return method.Name;
        }

        private static readonly string[] MethodAccessTypeLabels = { "CompilerControlled", "Private", "FamAndAssem", "Internal", "Protected", "FamOrAssem", "Public"};
        private static readonly (MethodSemanticsAttributes, string)[] SemanticsToCheck =
        {
            (MethodSemanticsAttributes.Setter, "_set"),
            (MethodSemanticsAttributes.Getter, "_get"),
            (MethodSemanticsAttributes.Other, "_oth"),
            (MethodSemanticsAttributes.AddOn, "_add"),
            (MethodSemanticsAttributes.RemoveOn, "_rem"),
            (MethodSemanticsAttributes.Fire, "_fire"),
        };
        private string ProduceMethodSignatureBase()
        {
            var method = OriginalMethod;
            
            var name = method.Name;
            if (method.Name.IsObfuscated(DeclaringType.AssemblyContext.GlobalContext.Options))
                name = "Method";

            if (name.IsInvalidInSource())
                name = name.FilterInvalidInSourceChars();

            if (method.Name == "GetType" && method.Parameters.Count == 0)
                name = "GetIl2CppType";

            var builder = new StringBuilder();
            builder.Append(name);
            builder.Append('_');
            builder.Append(MethodAccessTypeLabels[(int) (method.Attributes & MethodAttributes.MemberAccessMask)]);
            if (method.IsAbstract) builder.Append("_Abstract");
            if (method.IsVirtual) builder.Append("_Virtual");
            if (method.IsStatic) builder.Append("_Static");
            if (method.IsFinal) builder.Append("_Final");
            if (method.IsNewSlot) builder.Append("_New");
            foreach (var (semantic, str) in SemanticsToCheck)
                if ((semantic & method.SemanticsAttributes) != 0)
                    builder.Append(str);

            builder.Append('_');
            builder.Append(DeclaringType.AssemblyContext.RewriteTypeRef(method.ReturnType).GetUnmangledName());
            
            foreach (var param in method.Parameters)
            {
                builder.Append('_');
                builder.Append(DeclaringType.AssemblyContext.RewriteTypeRef(param.ParameterType).GetUnmangledName());
            }
            
            var address = Rva;
            if (address != 0 && Pass15GenerateMemberContexts.HasObfuscatedMethods && !Pass16ScanMethodRefs.NonDeadMethods.Contains(address)) builder.Append("_PDM");

            return builder.ToString();
        }

        
        private string UnmangleMethodNameWithSignature()
        {
            var unmangleMethodNameWithSignature = ProduceMethodSignatureBase() + "_" + DeclaringType.Methods.Where(ParameterSignatureMatchesThis).TakeWhile(it => it != this).Count();
            if (DeclaringType.AssemblyContext.GlobalContext.Options.RenameMap.TryGetValue(
                DeclaringType.NewType.GetNamespacePrefix() + "::" + unmangleMethodNameWithSignature, out var newName))
                unmangleMethodNameWithSignature = newName;
            return unmangleMethodNameWithSignature;
        }
        
        private bool ParameterSignatureMatchesThis(MethodRewriteContext otherRewriteContext)
        {
            var aM = otherRewriteContext.OriginalMethod;
            var bM = OriginalMethod;
            
            if (!otherRewriteContext.OriginalNameObfuscated)
                return false;
            
            var comparisonMask = MethodAttributes.MemberAccessMask | MethodAttributes.Static | MethodAttributes.Final |
                                 MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            if ((aM.Attributes & comparisonMask) !=
                (bM.Attributes & comparisonMask))
                return false;

            if (aM.SemanticsAttributes != bM.SemanticsAttributes)
                return false;

            if (aM.ReturnType.FullName != bM.ReturnType.FullName)
                return false;

            var a = aM.Parameters;
            var b = bM.Parameters;
            
            if (a.Count != b.Count)
                return false;

            for (var i = 0; i < a.Count; i++)
            {
                if (a[i].ParameterType.FullName != b[i].ParameterType.FullName)
                    return false;
            }

            if (Pass15GenerateMemberContexts.HasObfuscatedMethods)
            {
                var addressA = otherRewriteContext.Rva;
                var addressB = Rva;
                if (addressA != 0 && addressB != 0)
                    if (Pass16ScanMethodRefs.NonDeadMethods.Contains(addressA) != Pass16ScanMethodRefs.NonDeadMethods.Contains(addressB))
                        return false;
            }

            return true;
        }
    }
}