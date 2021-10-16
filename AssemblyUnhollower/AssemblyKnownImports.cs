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

        public TypeReference Void { get; }
        public TypeReference IntPtr { get; }
        public TypeReference String { get; }
        public TypeReference Int { get; }
        public TypeReference Long { get; }
        public TypeDefinition Type { get; }
        public TypeReference Enum { get; }
        public TypeReference Delegate { get; }
        public TypeReference MulticastDelegate { get; }
        public TypeReference ValueType { get; }
        public TypeReference Object { get; }
        public TypeReference Il2CppClassPointerStore { get; }
        public TypeReference Il2CppObjectBase { get; }
        public TypeReference Il2CppReferenceArray { get; }
        public TypeReference Il2CppStructArray { get; }
        public TypeReference Il2CppStringArray { get; }
        public TypeReference Il2CppArrayBase { get; }
        public TypeReference Il2CppArrayBaseSelfSubst { get; }
        public TypeReference DefaultMemberAttribute { get; }

        public MethodReference Il2CppObjectBaseToPointer { get; }
        public MethodReference Il2CppObjectBaseToPointerNotNull { get; }
        public MethodReference StringToNative { get; }
        public MethodReference StringFromNative { get; }
        public MethodReference Il2CppObjectCast { get; }
        public MethodReference Il2CppObjectTryCast { get; }
        public MethodReference Il2CppResolveICall { get; }
        public MethodReference WriteFieldWBarrier { get; }
        
        public MethodReference FieldGetOffset { get; }
        public MethodReference FieldStaticGet { get; }
        public MethodReference FieldStaticSet { get; }
        
        public MethodReference RuntimeInvoke { get; }
        public MethodReference RuntimeClassInit { get; }
        public MethodReference ObjectUnbox { get; }
        public MethodReference ObjectBox { get; }
        public MethodReference ValueSizeGet { get; }
        public MethodReference ClassIsValueType { get; }
        public MethodReference ObjectGetClass { get; }
        public MethodReference RaiseExceptionIfNecessary { get; }
        public MethodReference GetVirtualMethod { get; }
        public MethodReference GetFieldPointer { get; }
        public MethodReference GetIl2CppNestedClass { get; }
        public MethodReference GetIl2CppGlobalClass { get; }
        public MethodReference GetIl2CppMethod { get; }
        public MethodReference GetIl2CppMethodFromToken { get; }
        public MethodReference GetIl2CppTypeFromClass { get; }
        public MethodReference GetIl2CppTypeToClass { get; }
        public MethodReference Il2CppNewObject { get; }
        public MethodReference Il2CppMethodInfoToReflection { get; }
        public MethodReference Il2CppMethodInfoFromReflection { get; }
        public MethodReference Il2CppPointerToGeneric { get; }
        public MethodReference Il2CppRenderTypeNameGeneric { get; }
        
        public MethodReference LdTokUnstrippedImpl 
        {
			get
			{
                var declaringTypeRef = Module.ImportReference(typeof(RuntimeReflectionHelper));
                var returnTypeRef = Module.ImportReference(myContext.GetAssemblyByName("mscorlib").NewAssembly.MainModule.GetType("Il2CppSystem.RuntimeTypeHandle"));
                var methodReference = new MethodReference("GetRuntimeTypeHandle", returnTypeRef, declaringTypeRef) { HasThis = false };
                methodReference.GenericParameters.Add(new GenericParameter("T", methodReference));
                return Module.ImportReference(methodReference);
            }
        }
        
        public MethodReference FlagsAttributeCtor { get; }
        public MethodReference ObsoleteAttributeCtor { get; }
        public MethodReference NotSupportedExceptionCtor { get; }
        public MethodReference ObfuscatedNameAttributeCtor { get; }
        public MethodReference CallerCountAttributeCtor { get; }
        public MethodReference CachedScanResultsAttributeCtor { get; }
        public MethodReference ExtensionAttributeCtor { get; }
        

        public AssemblyKnownImports(ModuleDefinition module, RewriteGlobalContext context)
        {
            Module = module;
            myContext = context;
            
            Void = Module.ImportReference(TargetTypeSystemHandler.Void);
            IntPtr = Module.ImportReference(TargetTypeSystemHandler.IntPtr);
            String = Module.ImportReference(TargetTypeSystemHandler.String);
            Int = Module.ImportReference(TargetTypeSystemHandler.Int);
            Long = Module.ImportReference(TargetTypeSystemHandler.Long);
            Type = TargetTypeSystemHandler.Type;
            Enum = Module.ImportReference(TargetTypeSystemHandler.Enum);
            Delegate = Module.ImportReference(TargetTypeSystemHandler.Delegate);
            MulticastDelegate = Module.ImportReference(TargetTypeSystemHandler.MulticastDelegate);
            ValueType = Module.ImportReference(TargetTypeSystemHandler.ValueType);
            Object = Module.ImportReference(TargetTypeSystemHandler.Object);
            Il2CppClassPointerStore = Module.ImportReference(typeof(Il2CppClassPointerStore<>));
            Il2CppReferenceArray = Module.ImportReference(typeof(Il2CppReferenceArray<>));
            Il2CppStructArray = Module.ImportReference(typeof(Il2CppStructArray<>));
            Il2CppStringArray = Module.ImportReference(typeof(Il2CppStringArray));
            Il2CppArrayBase = Module.ImportReference(typeof(Il2CppArrayBase<>));
            Il2CppArrayBaseSelfSubst = Module.ImportReference(new GenericInstanceType(Il2CppArrayBase) { GenericArguments = { Il2CppArrayBase.GenericParameters[0] }});
            Il2CppObjectBase = Module.ImportReference(typeof(Il2CppObjectBase));
            DefaultMemberAttribute = Module.ImportReference(TargetTypeSystemHandler.DefaultMemberAttribute);
            // Il2CppObjectReference = Module.ImportReference(TargetTypeSystemHandler.Object);// todo!

            Il2CppObjectBaseToPointer = Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppObjectBaseToPtr"));
            Il2CppObjectBaseToPointerNotNull = Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppObjectBaseToPtrNotNull"));
            StringFromNative = Module.ImportReference(typeof(IL2CPP).GetMethod("Il2CppStringToManaged"));
            StringToNative = Module.ImportReference(typeof(IL2CPP).GetMethod("ManagedStringToIl2Cpp"));
            Il2CppObjectCast = Module.ImportReference(typeof(Il2CppObjectBase).GetMethod("Cast"));
            Il2CppObjectTryCast = Module.ImportReference(typeof(Il2CppObjectBase).GetMethod("TryCast"));
            Il2CppResolveICall = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.ResolveICall)));
            WriteFieldWBarrier = Module.ImportReference(myContext.HasGcWbarrierFieldWrite
                    ? typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_gc_wbarrier_set_field))
                    : typeof(IL2CPP).GetMethod(nameof(IL2CPP.FieldWriteWbarrierStub)));
            
            FieldGetOffset = Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_get_offset"));
            FieldStaticGet = Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_static_get_value"));
            FieldStaticSet = Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_field_static_set_value"));
            
            RuntimeInvoke = Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_runtime_invoke"));
            RuntimeClassInit = Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_runtime_class_init"));
            ObjectUnbox = Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_object_unbox"));
            ObjectBox = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_value_box)));
            ValueSizeGet = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_value_size)));
            ObjectGetClass = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_object_get_class)));
            ClassIsValueType = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_is_valuetype)));
            RaiseExceptionIfNecessary = Module.ImportReference(typeof(Il2CppException).GetMethod("RaiseExceptionIfNecessary"));
            GetVirtualMethod = Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_object_get_virtual_method"));
            GetFieldPointer = Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppField"));
            GetIl2CppNestedClass = Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppNestedType"));
            GetIl2CppGlobalClass = Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppClass"));
            GetIl2CppMethod = Module.ImportReference(typeof(IL2CPP).GetMethod("GetIl2CppMethod"));
            GetIl2CppMethodFromToken = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.GetIl2CppMethodByToken)));
            GetIl2CppTypeFromClass = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_get_type)));
            GetIl2CppTypeToClass = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_class_from_type)));
            Il2CppNewObject = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_object_new)));
            Il2CppMethodInfoFromReflection = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_method_get_from_reflection)));
            Il2CppMethodInfoToReflection = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_method_get_object)));
            Il2CppPointerToGeneric = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.PointerToValueGeneric)));
            Il2CppRenderTypeNameGeneric = Module.ImportReference(typeof(IL2CPP).GetMethod(nameof(IL2CPP.RenderTypeName), new [] {typeof(bool)}));
            


            FlagsAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(TargetTypeSystemHandler.FlagsAttribute)) { HasThis = true};
            ObsoleteAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(TargetTypeSystemHandler.ObsoleteAttribute))
                    {HasThis = true, Parameters = {new ParameterDefinition(String)}};
            
            NotSupportedExceptionCtor = new MethodReference(".ctor", Void, Module.ImportReference(TargetTypeSystemHandler.NotSupportedException))
                    {HasThis = true, Parameters = {new ParameterDefinition(String)}};
            
            ObfuscatedNameAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(typeof(ObfuscatedNameAttribute)))
                    {HasThis = true, Parameters = {new ParameterDefinition(String)}};
            
            CallerCountAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(typeof(CallerCountAttribute)))
                    {HasThis = true, Parameters = {new ParameterDefinition(Int)}};
            
            CachedScanResultsAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(typeof(CachedScanResultsAttribute)))
                    {HasThis = true, Parameters = {}};

            ExtensionAttributeCtor = new MethodReference(".ctor", Void, Module.ImportReference(typeof(ExtensionAttribute))) { HasThis = true };
        }
    }
}