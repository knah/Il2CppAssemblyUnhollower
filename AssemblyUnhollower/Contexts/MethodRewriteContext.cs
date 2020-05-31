using System.Linq;
using System.Text;
using Mono.Cecil;

namespace AssemblyUnhollower.Contexts
{
    public class MethodRewriteContext
    {
        public readonly TypeRewriteContext DeclaringType;
        public readonly MethodDefinition OriginalMethod;
        public readonly MethodDefinition NewMethod;

        public string UnmangledName { get; private set; }
        public string UnmangledNameWithSignature { get; private set; }
        
        public TypeDefinition? GenericInstantiationsStore { get; private set; }
        public TypeReference? GenericInstantiationsStoreSelfSubstRef { get; private set; }
        public TypeReference? GenericInstantiationsStoreSelfSubstMethodRef { get; private set; }
        public FieldReference NonGenericMethodInfoPointerField { get; private set; }

        public MethodRewriteContext(TypeRewriteContext declaringType, MethodDefinition originalMethod)
        {
            DeclaringType = declaringType;
            OriginalMethod = originalMethod;

            var newMethod = new MethodDefinition("", AdjustAttributes(originalMethod.Attributes), declaringType.AssemblyContext.Imports.Void);
            NewMethod = newMethod;
            
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
        }

        public void CtorPhase2()
        {
            UnmangledName = UnmangleMethodName(OriginalMethod);
            UnmangledNameWithSignature = UnmangleMethodNameWithSignature(OriginalMethod);

            NewMethod.Name = UnmangledName;
            NewMethod.ReturnType = DeclaringType.AssemblyContext.RewriteTypeRef(OriginalMethod.ReturnType);
                
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
                var genericMethodInfoStoreType = new TypeDefinition("", "MethodInfoStoreGeneric_" + UnmangledNameWithSignature + "`" + genericParams.Count, TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, DeclaringType.AssemblyContext.Imports.Object);
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
                    
                    foreach (var oldConstraint in oldParameter.Constraints)
                    {
                        if (oldConstraint.ConstraintType.FullName == "System.ValueType" || oldConstraint.ConstraintType.Resolve()?.IsInterface == true) continue;
                        
                        newParameter.Constraints.Add(new GenericParameterConstraint(
                            DeclaringType.AssemblyContext.RewriteTypeRef(oldConstraint.ConstraintType)));
                    }
                }

                var pointerField = new FieldDefinition("Pointer", FieldAttributes.Assembly | FieldAttributes.Static, DeclaringType.AssemblyContext.Imports.IntPtr);
                genericMethodInfoStoreType.Fields.Add(pointerField);

                GenericInstantiationsStoreSelfSubstRef = DeclaringType.NewType.Module.ImportReference(selfSubstRef);
                GenericInstantiationsStoreSelfSubstMethodRef = DeclaringType.NewType.Module.ImportReference(selfSubstMethodRef);
            }
            
            DeclaringType.NewType.Methods.Add(NewMethod);
        }

        private MethodAttributes AdjustAttributes(MethodAttributes original)
        {
            original &= ~(MethodAttributes.MemberAccessMask); // todo: handle Object overload correctly
            original &= ~(MethodAttributes.PInvokeImpl);
            original &= ~(MethodAttributes.Abstract);
            original &= ~(MethodAttributes.Virtual);
            original &= ~(MethodAttributes.Final);
            original &= ~(MethodAttributes.NewSlot);
            original &= ~(MethodAttributes.ReuseSlot);
            original |= MethodAttributes.Public;
            return original;
        }

        private string UnmangleMethodName(MethodDefinition method)
        {
            if(method.Name.IsObfuscated() && method.Name != ".ctor")
                return UnmangleMethodNameWithSignature(method);

            if (method.Name == "GetType" && method.Parameters.Count == 0)
                return "GetIl2CppType";
            
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
        private string ProduceMethodSignatureBase(MethodDefinition method)
        {
            var name = method.Name;
            if (method.Name.IsObfuscated())
                name = "Method";

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

            return builder.ToString();
        }

        
        private string UnmangleMethodNameWithSignature(MethodDefinition method)
        {
            return ProduceMethodSignatureBase(method) + "_" + method.DeclaringType.Methods.Where(it => ParameterSignatureSame(it, method)).TakeWhile(it => it != method).Count();
        }
        
        private static bool ParameterSignatureSame(MethodDefinition aM, MethodDefinition bM)
        {
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

            return true;
        }
    }
}