using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace AssemblyUnhollower.Contexts
{
    public class TypeRewriteContext
    {
        public readonly AssemblyRewriteContext AssemblyContext;
        public readonly TypeDefinition OriginalType;
        public readonly TypeDefinition NewType;
        public readonly int Il2CppToken;

        public readonly bool OriginalNameWasObfuscated;

        public FieldReference ClassPointerFieldRef { get; private set; }
        public TypeReference SelfSubstitutedRef { get; private set; }

        public TypeSpecifics ComputedTypeSpecifics;

        private readonly Dictionary<FieldDefinition, FieldRewriteContext> myFieldContexts = new Dictionary<FieldDefinition, FieldRewriteContext>();
        private readonly Dictionary<MethodDefinition, MethodRewriteContext> myMethodContexts = new Dictionary<MethodDefinition, MethodRewriteContext>();
        private readonly Dictionary<string, MethodRewriteContext> myMethodContextsByName = new Dictionary<string, MethodRewriteContext>();

        public IEnumerable<FieldRewriteContext> Fields => myFieldContexts.Values;
        public IEnumerable<MethodRewriteContext> Methods => myMethodContexts.Values;

        public TypeRewriteContext(AssemblyRewriteContext assemblyContext, TypeDefinition originalType, TypeDefinition newType)
        {
            AssemblyContext = assemblyContext ?? throw new ArgumentNullException(nameof(assemblyContext));
            OriginalType = originalType;
            NewType = newType ?? throw new ArgumentNullException(nameof(newType));

            if (OriginalType == null) return;
            
            OriginalNameWasObfuscated = OriginalType.Name != NewType.Name;
            if (OriginalNameWasObfuscated)
                NewType.CustomAttributes.Add(new CustomAttribute(assemblyContext.Imports.ObfuscatedNameAttributeCtor)
                    {ConstructorArguments = {new CustomAttributeArgument(assemblyContext.Imports.String, originalType.FullName)}});

            if (!OriginalType.IsValueType)
                ComputedTypeSpecifics = TypeSpecifics.ReferenceType;
            else if (OriginalType.IsEnum)
                ComputedTypeSpecifics = TypeSpecifics.BlittableStruct;
            else if (OriginalType.HasGenericParameters)
                ComputedTypeSpecifics = TypeSpecifics.NonBlittableStruct; // not reference type, covered by first if
        }

        public void AddMembers()
        {
            if (NewType.HasGenericParameters)
            {
                var genericInstanceType = new GenericInstanceType(NewType);
                foreach (var newTypeGenericParameter in NewType.GenericParameters)
                    genericInstanceType.GenericArguments.Add(newTypeGenericParameter);
                SelfSubstitutedRef = NewType.Module.ImportReference(genericInstanceType);
                var genericTypeRef = new GenericInstanceType(AssemblyContext.Imports.Il2CppClassPointerStore)
                    {GenericArguments = {SelfSubstitutedRef}};
                ClassPointerFieldRef = new FieldReference("NativeClassPtr", AssemblyContext.Imports.IntPtr,
                    NewType.Module.ImportReference(genericTypeRef));
            }
            else
            {
                SelfSubstitutedRef = NewType;
                var genericTypeRef = new GenericInstanceType(AssemblyContext.Imports.Il2CppClassPointerStore);
                if(OriginalType.IsPrimitive || OriginalType.FullName == "System.String")
                    genericTypeRef.GenericArguments.Add(NewType.Module.ImportReference(TargetTypeSystemHandler.Type.Module.GetType(OriginalType.FullName)));
                else
                    genericTypeRef.GenericArguments.Add(SelfSubstitutedRef);
                ClassPointerFieldRef = new FieldReference("NativeClassPtr", AssemblyContext.Imports.IntPtr,
                    NewType.Module.ImportReference(genericTypeRef));
            }

            if (OriginalType.IsEnum) return;

            var renamedFieldCounts = new Dictionary<string, int>();
            
            foreach (var originalTypeField in OriginalType.Fields)
                myFieldContexts[originalTypeField] = new FieldRewriteContext(this, originalTypeField, renamedFieldCounts);

            foreach (var originalTypeMethod in OriginalType.Methods)
            {
                if (originalTypeMethod.Name == ".cctor") continue;
                if (originalTypeMethod.Name == ".ctor" && originalTypeMethod.Parameters.Count == 1 &&
                    originalTypeMethod.Parameters[0].ParameterType.FullName == "System.IntPtr") continue;

                var methodRewriteContext = new MethodRewriteContext(this, originalTypeMethod);
                myMethodContexts[originalTypeMethod] = methodRewriteContext;
                myMethodContextsByName[originalTypeMethod.Name] = methodRewriteContext;
            }
        }

        public FieldRewriteContext GetFieldByOldField(FieldDefinition field) => myFieldContexts[field];
        public MethodRewriteContext GetMethodByOldMethod(MethodDefinition method) => myMethodContexts[method];
        public MethodRewriteContext? TryGetMethodByOldMethod(MethodDefinition method) => myMethodContexts.TryGetValue(method, out var result) ? result : null;
        public MethodRewriteContext? TryGetMethodByName(string name) => myMethodContextsByName.TryGetValue(name, out var result) ? result : null;
        public MethodRewriteContext? TryGetMethodByUnityAssemblyMethod(MethodDefinition method)
        {
            foreach (var methodRewriteContext in myMethodContexts)
            {
                var originalMethod = methodRewriteContext.Value.OriginalMethod;
                if (originalMethod.Name != method.Name) continue;
                if (originalMethod.Parameters.Count != method.Parameters.Count) continue;
                var badMethod = false;
                for (var i = 0; i < originalMethod.Parameters.Count; i++)
                {
                    if (originalMethod.Parameters[i].ParameterType.FullName != method.Parameters[i].ParameterType.FullName)
                    {
                        badMethod = true;
                        break;
                    }
                }
                if (badMethod) continue;

                return methodRewriteContext.Value;
            }

            return null;
        }
        
        public FieldRewriteContext? TryGetFieldByUnityAssemblyField(FieldDefinition field)
        {
            foreach (var fieldRewriteContext in myFieldContexts)
            {
                var originalField = fieldRewriteContext.Value.OriginalField;
                if (originalField.Name != field.Name) continue;

                if (originalField.FieldType.FullName != field.FieldType.FullName)
                    continue;

                return fieldRewriteContext.Value;
            }

            return null;
        }

        public enum TypeSpecifics
        {
            NotComputed,
            Computing,
            ReferenceType,
            BlittableStruct,
            NonBlittableStruct
        }
    }
}