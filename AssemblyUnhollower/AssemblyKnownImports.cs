using System;
using System.Collections.Generic;
using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Attributes;
using UnhollowerRuntimeLib;

namespace AssemblyUnhollower
{
    public class AssemblyKnownImports
    {
        private static readonly Dictionary<ModuleDefinition, AssemblyKnownImports> AssemblyMap = new Dictionary<ModuleDefinition, AssemblyKnownImports>();

        public static AssemblyKnownImports For(ModuleDefinition module, RewriteGlobalContext context)
        {
            return AssemblyMap.GetOrCreate(module, mod => new AssemblyKnownImports(mod, context));
        }

        public readonly ModuleDefinition Module;
        private readonly RewriteGlobalContext myContext;

        private readonly Lazy<TypeReference> myVoidReference;
        private readonly Lazy<TypeReference> myIntPtrReference;
        private readonly Lazy<TypeReference> myStringReference;
        private readonly Lazy<TypeDefinition> myTypeReference;
        private readonly Lazy<TypeReference> myEnumReference;
        private readonly Lazy<TypeReference> myDelegateReference;
        private readonly Lazy<TypeReference> myMulticastDelegateReference;
        private readonly Lazy<TypeReference> myValueTypeReference;
        private readonly Lazy<TypeReference> myObjectReference;
        private readonly Lazy<TypeReference> myIl2CppClassPointerStoreReference;
        private readonly Lazy<TypeReference> myIl2CppObjectBaseReference;
        private readonly Lazy<TypeReference> myIl2CppReferenceArray;
        private readonly Lazy<TypeReference> myIl2CppStructArray;
        private readonly Lazy<TypeReference> myIl2CppStringArray;
        private readonly Lazy<TypeReference> myIl2CppArrayBase;
        private readonly Lazy<TypeReference> myIl2CppArrayBaseSetlfSubst;
        private readonly Lazy<TypeReference> myDefaultMemberAttribute;

        public TypeReference Void => myVoidReference.Value;
        public TypeReference IntPtr => myIntPtrReference.Value;
        public TypeReference String => myStringReference.Value;
        public TypeDefinition Type => myTypeReference.Value;
        public TypeReference Enum => myEnumReference.Value;
        public TypeReference Delegate => myDelegateReference.Value;
        public TypeReference MulticastDelegate => myMulticastDelegateReference.Value;
        public TypeReference ValueType => myValueTypeReference.Value;
        public TypeReference Object => myObjectReference.Value;
        public TypeReference Il2CppClassPointerStore => myIl2CppClassPointerStoreReference.Value;
        public TypeReference Il2CppObjectBase => myIl2CppObjectBaseReference.Value;
        public TypeReference Il2CppReferenceArray => myIl2CppReferenceArray.Value;
        public TypeReference Il2CppStructArray => myIl2CppStructArray.Value;
        public TypeReference Il2CppStringArray => myIl2CppStringArray.Value;
        public TypeReference Il2CppArrayBase => myIl2CppArrayBase.Value;
        public TypeReference Il2CppArrayBaseSelfSubst => myIl2CppArrayBaseSetlfSubst.Value;
        public TypeReference DefaultMemberAttribute => myDefaultMemberAttribute.Value;

        public MethodReference Il2CppObjectBaseToPointer => myIl2CppObjectToPointer.Value;
        public MethodReference Il2CppObjectBaseToPointerNotNull => myIl2CppObjectToPointerNotNull.Value;
        public MethodReference StringToNative => myStringToNative.Value;
        public MethodReference StringFromNative => myStringFromNative.Value;
        public MethodReference Il2CppObjectCast => myIl2CppObjectCast.Value;
        public MethodReference Il2CppObjectTryCast => myIl2CppObjectTryCast.Value;
        public MethodReference Il2CppResolveICall => myIl2CppResolveICall.Value;

        private readonly Lazy<MethodReference> myIl2CppObjectToPointer;
        private readonly Lazy<MethodReference> myIl2CppObjectToPointerNotNull;
        private readonly Lazy<MethodReference> myStringToNative;
        private readonly Lazy<MethodReference> myStringFromNative;
        private readonly Lazy<MethodReference> myIl2CppObjectCast;
        private readonly Lazy<MethodReference> myIl2CppObjectTryCast;
        private readonly Lazy<MethodReference> myIl2CppResolveICall;
        
        private readonly Lazy<MethodReference> myFieldGetOffset;
        private readonly Lazy<MethodReference> myFieldStaticGet;
        private readonly Lazy<MethodReference> myFieldStaticSet;
        
        private readonly Lazy<MethodReference> myRuntimeInvoke;
        private readonly Lazy<MethodReference> myRuntimeClassInit;
        private readonly Lazy<MethodReference> myObjectUnbox;
        private readonly Lazy<MethodReference> myObjectBox;
        private readonly Lazy<MethodReference> myValueSizeGet;
        private readonly Lazy<MethodReference> myObjectGetClass;
        private readonly Lazy<MethodReference> myClassIsValueType;
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
        private readonly Lazy<MethodReference> myIl2CppPointerToGeneric;
        private readonly Lazy<MethodReference> myIl2CppRenderTypeNameGeneric;
        
        private readonly Lazy<MethodReference> myLdTokUnstrippedImpl;
        
        private readonly Lazy<MethodReference> myFlagsAttributeCtor;
        private readonly Lazy<MethodReference> myObsoleteAttributeCtor;
        private readonly Lazy<MethodReference> myNotSupportedExceptionCtor;
        private readonly Lazy<MethodReference> myObfuscatedNameAttributeCtor;
        
        public MethodReference FieldGetOffset => myFieldGetOffset.Value;
        public MethodReference FieldStaticGet => myFieldStaticGet.Value;
        public MethodReference FieldStaticSet => myFieldStaticSet.Value;
        
        public MethodReference RuntimeInvoke => myRuntimeInvoke.Value;
        public MethodReference RuntimeClassInit => myRuntimeClassInit.Value;
        public MethodReference ObjectUnbox => myObjectUnbox.Value;
        public MethodReference ObjectBox => myObjectBox.Value;
        public MethodReference ValueSizeGet => myValueSizeGet.Value;
        public MethodReference ClassIsValueType => myClassIsValueType.Value;
        public MethodReference ObjectGetClass => myObjectGetClass.Value;
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
        public MethodReference Il2CppPointerToGeneric => myIl2CppPointerToGeneric.Value;
        public MethodReference Il2CppRenderTypeNameGeneric => myIl2CppRenderTypeNameGeneric.Value;
        
        public MethodReference LdTokUnstrippedImpl => myLdTokUnstrippedImpl.Value;
        
        public MethodReference FlagsAttributeCtor => myFlagsAttributeCtor.Value;
        public MethodReference ObsoleteAttributeCtor => myObsoleteAttributeCtor.Value;
        public MethodReference NotSupportedExceptionCtor => myNotSupportedExceptionCtor.Value;
        public MethodReference ObfuscatedNameAttributeCtor => myObfuscatedNameAttributeCtor.Value;
        

        public AssemblyKnownImports(ModuleDefinition module, RewriteGlobalContext context)
        {
            Module = module;
            myContext = context;
            
            myVoidReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.Void));
            myIntPtrReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.IntPtr));
            myStringReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.String));
            myTypeReference = new Lazy<TypeDefinition>(() => TargetTypeSystemHandler.Type);
            myEnumReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.Enum));
            myDelegateReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.Delegate));
            myMulticastDelegateReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.MulticastDelegate));
            myValueTypeReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.ValueType));
            myObjectReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.Object));
            myIl2CppClassPointerStoreReference = new Lazy<TypeReference>(() => Module.ImportReference(typeof(Il2CppClassPointerStore<>)));
            myIl2CppReferenceArray = new Lazy<TypeReference>(() => Module.ImportReference(typeof(Il2CppReferenceArray<>)));
            myIl2CppStructArray = new Lazy<TypeReference>(() => Module.ImportReference(typeof(Il2CppStructArray<>)));
            myIl2CppStringArray = new Lazy<TypeReference>(() => Module.ImportReference(typeof(Il2CppStringArray)));
            myIl2CppArrayBase = new Lazy<TypeReference>(() => Module.ImportReference(typeof(Il2CppArrayBase<>)));
            myIl2CppArrayBaseSetlfSubst = new Lazy<TypeReference>(() => Module.ImportReference(new GenericInstanceType(Il2CppArrayBase) { GenericArguments = { Il2CppArrayBase.GenericParameters[0] }}));
            myIl2CppObjectBaseReference = new Lazy<TypeReference>(() => Module.ImportReference(typeof(Il2CppObjectBase)));
            myDefaultMemberAttribute = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.DefaultMemberAttribute));
            // myIl2CppObjectReference = new Lazy<TypeReference>(() => Module.ImportReference(TargetTypeSystemHandler.Object));// todo!
            
            myIl2CppObjectToPointer = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppObjectBaseToPtr")));
            myIl2CppObjectToPointerNotNull = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppObjectBaseToPtrNotNull")));
            myStringFromNative = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppStringToManaged")));
            myStringToNative = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("ManagedStringToIl2Cpp")));
            myIl2CppObjectCast = new Lazy<MethodReference>(() => Module.ImportReference(typeof(Il2CppObjectBase).GetMethod("Cast")));
            myIl2CppObjectTryCast = new Lazy<MethodReference>(() => Module.ImportReference(typeof(Il2CppObjectBase).GetMethod("TryCast")));
            myIl2CppResolveICall = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.ResolveICall))));
            
            myFieldGetOffset = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_get_offset")));
            myFieldStaticGet = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_static_get_value")));
            myFieldStaticSet = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_static_set_value")));
            
            myRuntimeInvoke = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_runtime_invoke")));
            myRuntimeClassInit = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_runtime_class_init")));
            myObjectUnbox = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_object_unbox")));
            myObjectBox = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_value_box))));
            myValueSizeGet = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_value_size))));
            myObjectGetClass = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_object_get_class))));
            myClassIsValueType = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_is_valuetype))));
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
            myIl2CppPointerToGeneric = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.PointerToValueGeneric))));
            myIl2CppRenderTypeNameGeneric = new Lazy<MethodReference>(() => Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.RenderTypeName), new [] {typeof(bool)})));
            
            myLdTokUnstrippedImpl = new Lazy<MethodReference>(() =>
            {
                var declaringTypeRef = Module.ImportReference(typeof(RuntimeReflectionHelper));
                var returnTypeRef = Module.ImportReference(myContext.GetAssemblyByName("mscorlib").NewAssembly.MainModule.GetType("Il2CppSystem.RuntimeTypeHandle"));
                var methodReference = new MethodReference("GetRuntimeTypeHandle", returnTypeRef, declaringTypeRef) { HasThis = false };
                methodReference.GenericParameters.Add(new GenericParameter("T", methodReference));
                return Module.ImportReference(methodReference);
            });
            
            myFlagsAttributeCtor = new Lazy<MethodReference>(() => new MethodReference(".ctor", Void, Module.ImportReference(TargetTypeSystemHandler.FlagsAttribute)) { HasThis = true});
            myObsoleteAttributeCtor = new Lazy<MethodReference>(() =>
                new MethodReference(".ctor", Void, Module.ImportReference(TargetTypeSystemHandler.ObsoleteAttribute))
                    {HasThis = true, Parameters = {new ParameterDefinition(String)}});
            
            myNotSupportedExceptionCtor = new Lazy<MethodReference>(() =>
                new MethodReference(".ctor", Void, Module.ImportReference(TargetTypeSystemHandler.NotSupportedException))
                    {HasThis = true, Parameters = {new ParameterDefinition(String)}});
            
            myObfuscatedNameAttributeCtor = new Lazy<MethodReference>(() =>
                new MethodReference(".ctor", Void, Module.ImportReference(typeof(ObfuscatedNameAttribute)))
                    {HasThis = true, Parameters = {new ParameterDefinition(String)}});
        }
    }
}