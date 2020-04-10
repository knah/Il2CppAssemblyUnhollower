using System;
using System.Collections.Generic;
using Mono.Cecil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower
{
    public class AssemblyKnownImports
    {
        private static readonly Dictionary<ModuleDefinition, AssemblyKnownImports> AssemblyMap = new Dictionary<ModuleDefinition, AssemblyKnownImports>();

        public static AssemblyKnownImports For(ModuleDefinition module)
        {
            return AssemblyMap.GetOrCreate(module, mod => new AssemblyKnownImports(mod));
        }

        public static AssemblyKnownImports For(TypeDefinition type) => For(type.Module);
        public static AssemblyKnownImports For(IMemberDefinition type) => For(type.DeclaringType.Module);
        
        public readonly ModuleDefinition Module;

        private readonly Lazy<TypeReference> myVoidReference;
        private readonly Lazy<TypeReference> myIntPtrReference;
        private readonly Lazy<TypeReference> myStringReference;
        private readonly Lazy<TypeDefinition> myTypeReference;
        private readonly Lazy<TypeReference> myEnumReference;
        private readonly Lazy<TypeReference> myValueTypeReference;
        private readonly Lazy<TypeReference> myObjectReference;
        private readonly Lazy<TypeReference> myIl2CppClassPointerStoreReference;
        private readonly Lazy<TypeReference> myIl2CppObjectBaseReference;
        private readonly Lazy<TypeReference> myIl2CppReferenceArray;

        public TypeReference Void => myVoidReference.Value;
        public TypeReference IntPtr => myIntPtrReference.Value;
        public TypeReference String => myStringReference.Value;
        public TypeDefinition Type => myTypeReference.Value;
        public TypeReference Enum => myEnumReference.Value;
        public TypeReference ValueType => myValueTypeReference.Value;
        public TypeReference Object => myObjectReference.Value;
        public TypeReference Il2CppClassPointerStore => myIl2CppClassPointerStoreReference.Value;
        public TypeReference Il2CppObjectBase => myIl2CppObjectBaseReference.Value;
        public TypeReference Il2CppReferenceArray => myIl2CppReferenceArray.Value;

        public MethodReference Il2CppObjectBaseToPointer => myIl2CppObjectToPointer.Value;
        public MethodReference Il2CppObjectBaseToPointerNotNull => myIl2CppObjectToPointerNotNull.Value;
        public MethodReference StringToNative => myStringToNative.Value;
        public MethodReference StringFromNative => myStringFromNative.Value;
        public MethodReference Il2CppObjectCast => myIl2CppObjectCast.Value;

        private readonly Lazy<MethodReference> myIl2CppObjectToPointer;
        private readonly Lazy<MethodReference> myIl2CppObjectToPointerNotNull;
        private readonly Lazy<MethodReference> myStringToNative;
        private readonly Lazy<MethodReference> myStringFromNative;
        private readonly Lazy<MethodReference> myIl2CppObjectCast;
        
        private readonly Lazy<MethodReference> myFieldGetOffset;
        private readonly Lazy<MethodReference> myFieldStaticGet;
        private readonly Lazy<MethodReference> myFieldStaticSet;
        
        private readonly Lazy<MethodReference> myRuntimeInvoke;
        private readonly Lazy<MethodReference> myRuntimeClassInit;
        private readonly Lazy<MethodReference> myObjectUnbox;
        private readonly Lazy<MethodReference> myRaiseExceptionIfNecessary;
        private readonly Lazy<MethodReference> myGetVirtualMethod;
        private readonly Lazy<MethodReference> myGetFieldPtr;
        private readonly Lazy<MethodReference> myGetIl2CppNestedClass;
        private readonly Lazy<MethodReference> myGetIl2CppGlobalClass;
        private readonly Lazy<MethodReference> myGetIl2CppMethod;
        private readonly Lazy<MethodReference> myGetIl2CppTypeFromClass;
        private readonly Lazy<MethodReference> myGetIl2CppTypeToClass;
        private readonly Lazy<MethodReference> myIl2CppNewObject;
        private readonly Lazy<MethodReference> myIl2CppMethodInfoToReflection;
        private readonly Lazy<MethodReference> myIl2CppMethodInfoFromReflection;
        
        public MethodReference FieldGetOffset => myFieldGetOffset.Value;
        public MethodReference FieldStaticGet => myFieldStaticGet.Value;
        public MethodReference FieldStaticSet => myFieldStaticSet.Value;
        
        public MethodReference RuntimeInvoke => myRuntimeInvoke.Value;
        public MethodReference RuntimeClassInit => myRuntimeClassInit.Value;
        public MethodReference ObjectUnbox => myObjectUnbox.Value;
        public MethodReference RaiseExceptionIfNecessary => myRaiseExceptionIfNecessary.Value;
        public MethodReference GetVirtualMethod => myGetVirtualMethod.Value;
        public MethodReference GetFieldPointer => myGetFieldPtr.Value;
        public MethodReference GetIl2CppNestedClass => myGetIl2CppNestedClass.Value;
        public MethodReference GetIl2CppGlobalClass => myGetIl2CppGlobalClass.Value;
        public MethodReference GetIl2CppMethod => myGetIl2CppMethod.Value;
        public MethodReference GetIl2CppTypeFromClass => myGetIl2CppTypeFromClass.Value;
        public MethodReference GetIl2CppTypeToClass => myGetIl2CppTypeToClass.Value;
        public MethodReference Il2CppNewObject => myIl2CppNewObject.Value;
        public MethodReference Il2CppMethodInfoToReflection => myIl2CppMethodInfoToReflection.Value;
        public MethodReference Il2CppMethodInfoFromReflection => myIl2CppMethodInfoFromReflection.Value;
        

        public AssemblyKnownImports(ModuleDefinition module)
        {
            Module = module;
            myVoidReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.Void));
            myIntPtrReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.IntPtr));
            myStringReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.String));
            myTypeReference = new Lazy<TypeDefinition>(() => TargetTypeSystemHandler.Type);
            myEnumReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.Enum));
            myValueTypeReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.ValueType));
            myObjectReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.Object));
            myIl2CppClassPointerStoreReference = new Lazy<TypeReference>(() => Module.ImportReference(typeof(Il2CppClassPointerStore<>)));
            myIl2CppReferenceArray = new Lazy<TypeReference>(() => Module.ImportReference(typeof(Il2CppReferenceArray<>)));
            myIl2CppObjectBaseReference = new Lazy<TypeReference>(() => Module.ImportReference(typeof(Il2CppObjectBase)));
            // myIl2CppObjectReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.Object));// todo!
            
            myIl2CppObjectToPointer = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppObjectBaseToPtr")));
            myIl2CppObjectToPointerNotNull = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppObjectBaseToPtrNotNull")));
            myStringFromNative = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppStringToManaged")));
            myStringToNative = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("ManagedStringToIl2Cpp")));
            myIl2CppObjectCast = new Lazy<MethodReference>(() => Module.ImportReference(typeof(Il2CppObjectBase).GetMethod("Cast")));
            
            myFieldGetOffset = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_get_offset")));
            myFieldStaticGet = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_static_get_value")));
            myFieldStaticSet = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_static_set_value")));
            
            myRuntimeInvoke = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_runtime_invoke")));
            myRuntimeClassInit = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_runtime_class_init")));
            myObjectUnbox = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_object_unbox")));
            myRaiseExceptionIfNecessary = new Lazy<MethodReference>(() => Module.ImportReference(typeof(Il2CppException).GetMethod("RaiseExceptionIfNecessary")));
            myGetVirtualMethod = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_object_get_virtual_method")));
            myGetFieldPtr = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppField")));
            myGetIl2CppNestedClass = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppNestedType")));
            myGetIl2CppGlobalClass = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppClass")));
            myGetIl2CppMethod = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppMethod")));
            myGetIl2CppTypeFromClass = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_get_type))));
            myGetIl2CppTypeToClass = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_from_type))));
            myIl2CppNewObject = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_object_new))));
            myIl2CppMethodInfoFromReflection = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_method_get_from_reflection))));
            myIl2CppMethodInfoToReflection = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_method_get_object))));
        }
    }
}