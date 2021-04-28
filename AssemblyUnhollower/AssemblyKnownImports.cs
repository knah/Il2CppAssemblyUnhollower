using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
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

        public readonly TypeReference Void;
        public readonly TypeReference IntPtr;
        public readonly TypeReference String;
        public readonly TypeReference Int;
        public readonly TypeReference UInt;
        public readonly TypeReference Long;
        public readonly TypeDefinition Type;
        public readonly TypeReference Enum;
        public readonly TypeReference Delegate;
        public readonly TypeReference MulticastDelegate;
        public readonly TypeReference ValueType;
        public readonly TypeReference Object;
        public readonly TypeReference Il2CppClassPointerStore;
        public readonly TypeReference Il2CppObjectBase;
        public readonly TypeReference Il2CppReferenceArray;
        public readonly TypeReference Il2CppStructArray;
        public readonly TypeReference Il2CppArrayBase;
        public readonly TypeReference Il2CppArrayBaseSelfSubst;
        public readonly TypeReference DefaultMemberAttribute;
        public readonly TypeReference Il2CppNonBlittableValueType;

        public readonly MethodReference Il2CppObjectBaseToPointer;
        public readonly MethodReference Il2CppObjectBaseToPointerNotNull;
        public readonly MethodReference StringToNative;
        public readonly MethodReference StringFromNative;
        public readonly MethodReference Il2CppObjectCast;
        public readonly MethodReference Il2CppObjectTryCast;
        public readonly MethodReference Il2CppResolveICall;

        public readonly MethodReference FieldGetOffset;
        public readonly MethodReference FieldStaticGet;
        public readonly MethodReference FieldStaticSet;
        
        public MethodReference WriteFieldWBarrier => myWriteFieldWBarrier.Value;
        private readonly Lazy<MethodReference> myWriteFieldWBarrier;
        
        public readonly MethodReference RuntimeInvoke;
        public readonly MethodReference RuntimeClassInit;
        public readonly MethodReference TypeFromToken;
        public readonly MethodReference RegisterTypeTokenExplicit;
        public readonly MethodReference ObjectUnbox;
        public readonly MethodReference ObjectBox;
        public readonly MethodReference ValueSizeGet;
        public readonly MethodReference ClassIsValueType;
        public readonly MethodReference ObjectGetClass;
        public readonly MethodReference RaiseExceptionIfNecessary;
        public readonly MethodReference GetVirtualMethod;
        public readonly MethodReference GetFieldPointer;
        public readonly MethodReference GetIl2CppNestedClass;
        public readonly MethodReference GetIl2CppGlobalClass;
        public readonly MethodReference GetIl2CppMethod;
        public readonly MethodReference GetIl2CppMethodFromToken;
        public readonly MethodReference GetIl2CppTypeFromClass;
        public readonly MethodReference GetIl2CppTypeToClass;
        public readonly MethodReference Il2CppNewObject;
        public readonly MethodReference Il2CppMethodInfoToReflection;
        public readonly MethodReference Il2CppMethodInfoFromReflection;
        // public readonly MethodReference Il2CppPointerToGeneric;
        public readonly MethodReference Il2CppRenderTypeNameGeneric;
        
        public MethodReference LdTokUnstrippedImpl => myLdTokUnstrippedImpl.Value;
        public readonly Lazy<MethodReference> myLdTokUnstrippedImpl;
        
        
        public readonly MethodReference FlagsAttributeCtor;
        public readonly MethodReference ObsoleteAttributeCtor;
        public readonly MethodReference NotSupportedExceptionCtor;
        public readonly MethodReference ObfuscatedNameAttributeCtor;
        public readonly MethodReference NativeTypeTokenAttributeCtor;
        public readonly MethodReference CallerCountAttributeCtor;
        public readonly MethodReference CachedScanResultsAttributeCtor;
        public readonly MethodReference ExtensionAttributeCtor;

        public readonly MethodReference ReadStaticFieldGeneric;
        public readonly MethodReference WriteStaticFieldGeneric;
        public readonly MethodReference ReadFieldGeneric;
        public readonly MethodReference WriteFieldGeneric;
        public readonly MethodReference MarshalMethodReturn;
        public readonly MethodReference MarshalMethodParameter;
        public readonly MethodReference MarshalMethodParameterByRef;
        public readonly MethodReference MarshalMethodParameterByRefRestore;
        public readonly MethodReference ScratchSpaceEnter;
        public readonly MethodReference ScratchSpaceLeave;

        public AssemblyKnownImports(ModuleDefinition module, RewriteGlobalContext context)
        {
            Module = module;
            myContext = context;
            
            Void =  Module.ImportReference(TargetTypeSystemHandler.Void);
            IntPtr =  Module.ImportReference(TargetTypeSystemHandler.IntPtr);
            String =  Module.ImportReference(TargetTypeSystemHandler.String);
            Int =  Module.ImportReference(TargetTypeSystemHandler.Int);
            UInt =  Module.ImportReference(TargetTypeSystemHandler.UInt);
            Long =  Module.ImportReference(TargetTypeSystemHandler.Long);
            Type =  TargetTypeSystemHandler.Type;
            Enum =  Module.ImportReference(TargetTypeSystemHandler.Enum);
            Delegate =  Module.ImportReference(TargetTypeSystemHandler.Delegate);
            MulticastDelegate =  Module.ImportReference(TargetTypeSystemHandler.MulticastDelegate);
            ValueType =  Module.ImportReference(TargetTypeSystemHandler.ValueType);
            Object =  Module.ImportReference(TargetTypeSystemHandler.Object);
            Il2CppClassPointerStore =  Module.ImportReference(typeof(Il2CppClassPointerStore<>));
            Il2CppReferenceArray =  Module.ImportReference(typeof(Il2CppReferenceArray<>));
            Il2CppStructArray =  Module.ImportReference(typeof(Il2CppStructArray<>));
            Il2CppArrayBase =  Module.ImportReference(typeof(Il2CppArrayBase<>));
            Il2CppArrayBaseSelfSubst =  Module.ImportReference(new GenericInstanceType(Il2CppArrayBase) { GenericArguments = { Il2CppArrayBase.GenericParameters[0] }});
            Il2CppObjectBase =  Module.ImportReference(typeof(Il2CppObjectBase));
            DefaultMemberAttribute =  Module.ImportReference(TargetTypeSystemHandler.DefaultMemberAttribute);
            
            // This can't be imported with typeof() because unholllower runs without Il2CppMscorlib
            var name = typeof(Il2CppObjectBase).Assembly.GetName();
            Il2CppNonBlittableValueType = Module.ImportReference(new TypeReference(nameof(UnhollowerBaseLib),
                nameof(UnhollowerBaseLib.Il2CppNonBlittableValueType), Module,
                new AssemblyNameReference(name.Name, name.Version)));
            // myIl2CppObject =  Module.ImportReference(TargetTypeSystemHandler.Object);// todo!
            
            Il2CppObjectBaseToPointer =  Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppObjectBaseToPtr"));
            Il2CppObjectBaseToPointerNotNull =  Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppObjectBaseToPtrNotNull"));
            StringFromNative =  Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppStringToManaged"));
            StringToNative =  Module.ImportReference(typeof(IL2CPP).GetMethod("ManagedStringToIl2Cpp"));
            Il2CppObjectCast =  Module.ImportReference(typeof(Il2CppObjectBase).GetMethod("Cast"));
            Il2CppObjectTryCast =  Module.ImportReference(typeof(Il2CppObjectBase).GetMethod("TryCast"));
            Il2CppResolveICall =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.ResolveICall)));
            myWriteFieldWBarrier = new Lazy<MethodReference>(() =>
                Module.ImportReference(myContext.HasGcWbarrierFieldWrite
                    ? typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_gc_wbarrier_set_field))
                    : typeof(IL2CPP).GetMethod(nameof(IL2CPP.FieldWriteWbarrierStub))));
            
            FieldGetOffset =  Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_get_offset"));
            FieldStaticGet =  Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_static_get_value"));
            FieldStaticSet =  Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_static_set_value"));
            
            RuntimeInvoke =  Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_runtime_invoke"));
            RuntimeClassInit =  Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_runtime_class_init"));
            TypeFromToken =  Module.ImportReference(typeof(Type).GetMethod(nameof(System.Type.GetTypeFromHandle)));
            RegisterTypeTokenExplicit =  Module.ImportReference(typeof(Il2CppClassPointerStore).GetMethod(nameof(UnhollowerBaseLib.Il2CppClassPointerStore.RegisterTypeWithExplicitTokenInfo)));
            ObjectUnbox =  Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_object_unbox"));
            ObjectBox =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_value_box)));
            ValueSizeGet =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_value_size)));
            ObjectGetClass =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_object_get_class)));
            ClassIsValueType =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_is_valuetype)));
            RaiseExceptionIfNecessary =  Module.ImportReference(typeof(Il2CppException).GetMethod("RaiseExceptionIfNecessary"));
            GetVirtualMethod =  Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_object_get_virtual_method"));
            GetFieldPointer =  Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppField"));
            GetIl2CppNestedClass =  Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppNestedType"));
            GetIl2CppGlobalClass =  Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppClass"));
            GetIl2CppMethod =  Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppMethod"));
            GetIl2CppMethodFromToken =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.GetIl2CppMethodByToken)));
            GetIl2CppTypeFromClass =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_get_type)));
            GetIl2CppTypeToClass =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_from_type)));
            Il2CppNewObject =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_object_new)));
            Il2CppMethodInfoFromReflection =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_method_get_from_reflection)));
            Il2CppMethodInfoToReflection =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_method_get_object)));
            // Il2CppPointerToGeneric =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.PointerToValueGeneric)));
            Il2CppRenderTypeNameGeneric =  Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.RenderTypeName), new [] {typeof(bool)}));

            ReadStaticFieldGeneric = Module.ImportReference(typeof(GenericMarshallingUtils).GetMethod(nameof(GenericMarshallingUtils.ReadStaticFieldGeneric)));
            WriteStaticFieldGeneric = Module.ImportReference(typeof(GenericMarshallingUtils).GetMethod(nameof(GenericMarshallingUtils.WriteStaticFieldGeneric)));
            ReadFieldGeneric = Module.ImportReference(typeof(GenericMarshallingUtils).GetMethod(nameof(GenericMarshallingUtils.ReadFieldGeneric)));
            WriteFieldGeneric = Module.ImportReference(typeof(GenericMarshallingUtils).GetMethod(nameof(GenericMarshallingUtils.WriteFieldGeneric)));
            MarshalMethodReturn = Module.ImportReference(typeof(GenericMarshallingUtils).GetMethod(nameof(GenericMarshallingUtils.MarshalGenericMethodReturn)));
            MarshalMethodParameter = Module.ImportReference(typeof(GenericMarshallingUtils).GetMethod(nameof(GenericMarshallingUtils.MarshalMethodParameter)));
            MarshalMethodParameterByRef = Module.ImportReference(typeof(GenericMarshallingUtils).GetMethod(nameof(GenericMarshallingUtils.MarshalMethodParameterByRef)));
            MarshalMethodParameterByRefRestore = Module.ImportReference(typeof(GenericMarshallingUtils).GetMethod(nameof(GenericMarshallingUtils.MarshalMethodParameterByRefRestore)));
            ScratchSpaceEnter = Module.ImportReference(typeof(MethodCallScratchSpaceAllocator).GetMethod(nameof(MethodCallScratchSpaceAllocator.EnterMethodCall)));
            ScratchSpaceLeave = Module.ImportReference(typeof(MethodCallScratchSpaceAllocator).GetMethod(nameof(MethodCallScratchSpaceAllocator.ExitMethodCall)));
            
            
            myLdTokUnstrippedImpl = new Lazy<MethodReference>(() =>
            {
                var declaringTypeRef = Module.ImportReference(typeof(RuntimeReflectionHelper));
                var returnTypeRef = Module.ImportReference(myContext.GetAssemblyByName("mscorlib").NewAssembly.MainModule.GetType("Il2CppSystem.RuntimeTypeHandle"));
                var methodReference = new MethodReference("GetRuntimeTypeHandle", returnTypeRef, declaringTypeRef) { HasThis = false };
                methodReference.GenericParameters.Add(new GenericParameter("T", methodReference));
                return Module.ImportReference(methodReference);
            });
            
            FlagsAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(TargetTypeSystemHandler.FlagsAttribute)) { HasThis = true };
            ObsoleteAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(TargetTypeSystemHandler.ObsoleteAttribute))
                    {HasThis = true, Parameters = {new ParameterDefinition(String)}};
            
            NotSupportedExceptionCtor = new MethodReference(".ctor", Void, Module.ImportReference(TargetTypeSystemHandler.NotSupportedException))
                    {HasThis = true, Parameters = {new ParameterDefinition(String)}};
            
            ObfuscatedNameAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(typeof(ObfuscatedNameAttribute)))
                    {HasThis = true, Parameters = {new ParameterDefinition(String)}};
            
            NativeTypeTokenAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(typeof(NativeTypeTokenAttribute)))
                {HasThis = true, Parameters = {}};
            
            CallerCountAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(typeof(CallerCountAttribute)))
                    {HasThis = true, Parameters = {new ParameterDefinition(Int)}};
            
            CachedScanResultsAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(typeof(CachedScanResultsAttribute)))
                    {HasThis = true, Parameters = {}};

            ExtensionAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(typeof(ExtensionAttribute))) { HasThis = true };
        }
    }
}