using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Runtime;
using ValueTuple = Il2CppSystem.ValueTuple;
using Void = Il2CppSystem.Void;

namespace UnhollowerRuntimeLib
{
    public unsafe static class ClassInjector
    {
        private static readonly Il2CppAssembly* FakeAssembly;
        private static readonly Il2CppImage* FakeImage;

        private static readonly HashSet<string> InjectedTypes = new HashSet<string>(); 

        static ClassInjector()
        {
            FakeAssembly = (Il2CppAssembly*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppAssembly>());
            FakeImage = (Il2CppImage*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImage>());

            *FakeAssembly = default;
            *FakeImage = default;

            FakeAssembly->image = FakeImage;
            FakeAssembly->aname.name = Marshal.StringToHGlobalAnsi("InjectedMonoTypes");
            
            FakeImage->assembly = FakeAssembly;
            FakeImage->dynamic = 1;
            FakeImage->name = FakeAssembly->aname.name;
            FakeImage->nameNoExt = FakeImage->name;
        }

        public static void ProcessNewObject(Il2CppObjectBase obj)
        {
            var pointer = obj.Pointer;
            var handle = GCHandle.Alloc(obj, GCHandleType.Normal);
            AssignGcHandle(pointer, handle);
        }

        public static IntPtr DerivedConstructorPointer<T>()
        {
            return IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<T>.NativeClassPtr); // todo: consider calling base constructor
        }
        
        public static void DerivedConstructorBody(Il2CppObjectBase objectBase)
        {
            var ownGcHandle = GCHandle.Alloc(objectBase, GCHandleType.Normal);
            AssignGcHandle(objectBase.Pointer, ownGcHandle);
        }
        
        public static void AssignGcHandle(IntPtr pointer, GCHandle gcHandle)
        {
            var handleAsPointer = GCHandle.ToIntPtr(gcHandle);
            if (pointer == IntPtr.Zero) throw new NullReferenceException(nameof(pointer));
            var objectKlass = (Il2CppClass*) IL2CPP.il2cpp_object_get_class(pointer);
            var targetGcHandlePointer = IntPtr.Add(pointer, (int) objectKlass->instance_size - IntPtr.Size);
            *(IntPtr*) targetGcHandlePointer = handleAsPointer;
        }

        public static void RegisterTypeInIl2Cpp<T>() where T : class
        {
            var type = typeof(T);
            
            if(type.IsGenericType || type.IsGenericTypeDefinition)
                throw new ArgumentException($"Type {type} is generic and can't be used in il2cpp");
            
            var currentPointer = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (currentPointer != IntPtr.Zero)
                throw new ArgumentException($"Type {type} is already registered in il2cpp");

            var baseType = type.BaseType;
            var baseClassPointer = (Il2CppClass*) ReadClassPointerForType(baseType);
            if (baseClassPointer == null)
                throw new ArgumentException($"Base class {baseType} of class {type} is not registered in il2cpp");
            
            if ((baseClassPointer->bitfield_1 & ClassBitfield1.valuetype) != 0 || (baseClassPointer->bitfield_1 & ClassBitfield1.enumtype) != 0)
                throw new ArgumentException($"Base class {baseType} is value type and can't be inherited from");
            
            if ((baseClassPointer->bitfield_1 & ClassBitfield1.is_generic) != 0)
                throw new ArgumentException($"Base class {baseType} is generic and can't be inherited from");
            
            if ((baseClassPointer->flags & Il2CppClassAttributes.TYPE_ATTRIBUTE_SEALED) != 0)
                throw new ArgumentException($"Base class {baseType} is sealed and can't be inherited from");
            
            if ((baseClassPointer->flags & Il2CppClassAttributes.TYPE_ATTRIBUTE_INTERFACE) != 0)
                throw new ArgumentException($"Base class {baseType} is an interface and can't be inherited from");
            
            lock (InjectedTypes)
                if (!InjectedTypes.Add(typeof(T).FullName))
                    throw new ArgumentException($"Type with FullName {typeof(T).FullName} is already injected. Don't inject the same type twice, or use a different namespace");
            
            if (ourOriginalTypeToClassMethod == null)
                HookClassFromType();

            var allocSize = Marshal.SizeOf<Il2CppClass>() + baseClassPointer->vtable_count * IntPtr.Size * 2;
            var classPointer = (Il2CppClass*) Marshal.AllocHGlobal(allocSize);
            *classPointer = default;

            classPointer->image = FakeImage;
            classPointer->parent = baseClassPointer;
            classPointer->element_class = classPointer->klass = classPointer->castClass = classPointer;
            classPointer->native_size = -1;
            classPointer->actualSize = classPointer->instance_size = baseClassPointer->instance_size + (uint) IntPtr.Size;
            classPointer->bitfield_1 = ClassBitfield1.initialized | ClassBitfield1.initialized_and_no_error |
                                       ClassBitfield1.size_inited;
            classPointer->bitfield_2 = ClassBitfield2.has_finalize | ClassBitfield2.is_vtable_initialized;
            classPointer->name = Marshal.StringToHGlobalAnsi(type.Name);
            classPointer->namespaze = Marshal.StringToHGlobalAnsi(type.Namespace);
            
            classPointer->this_arg.type = classPointer->byval_arg.type = Il2CppTypeEnum.IL2CPP_TYPE_CLASS;
            classPointer->this_arg.mods_byref_pin = 64;

            classPointer->flags = baseClassPointer->flags; // todo: adjust flags?

            var eligibleMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(IsMethodEligible).ToArray();
            var methodCount = 2 + eligibleMethods.Length; // 1 is the finalizer, 1 is empty ctor

            classPointer->method_count = (ushort) methodCount;
            var methodPointerArray = (Il2CppMethodInfo**) Marshal.AllocHGlobal(methodCount * IntPtr.Size);
            classPointer->methods = methodPointerArray;

            methodPointerArray[0] = ConvertStaticMethod(FinalizeDelegate, "Finalize", classPointer);
            methodPointerArray[1] = ConvertStaticMethod(CreateEmptyCtor(type), ".ctor", classPointer);
            for (var i = 0; i < eligibleMethods.Length; i++)
            {
                var methodInfo = eligibleMethods[i];
                methodPointerArray[i + 2] = ConvertMethodInfo(methodInfo, classPointer);
            }

            var vTablePointer = (VirtualInvokeData*) IntPtr.Add((IntPtr) classPointer, Marshal.SizeOf<Il2CppClass>());
            var baseVTablePointer = (VirtualInvokeData*) IntPtr.Add((IntPtr) baseClassPointer, Marshal.SizeOf<Il2CppClass>());
            classPointer->vtable_count = baseClassPointer->vtable_count;
            for (var i = 0; i < classPointer->vtable_count; i++)
            {
                vTablePointer[i] = baseVTablePointer[i];
                if (Marshal.PtrToStringAnsi(vTablePointer[i].method->name) == "Finalize") // slot number is not static
                {
                    vTablePointer[i].method = methodPointerArray[0];
                    vTablePointer[i].methodPtr = methodPointerArray[0]->methodPointer;
                }
            }

            var newCounter = Interlocked.Decrement(ref ourClassOverrideCounter);
            FakeTokenClasses[newCounter] = (IntPtr) classPointer;
            classPointer->byval_arg.data = classPointer->this_arg.data = (IntPtr) newCounter;

            RuntimeSpecificsStore.SetClassInfo((IntPtr) classPointer, true, true);
            Il2CppClassPointerStore<T>.NativeClassPtr = (IntPtr) classPointer;
            
            LogSupport.Info($"Registered mono type {typeof(T)} in il2cpp domain");
        }

        private static IntPtr ReadClassPointerForType(Type type)
        {
            if (type == typeof(void)) return Il2CppClassPointerStore<Void>.NativeClassPtr;
            return (IntPtr) typeof(Il2CppClassPointerStore<>).MakeGenericType(type)
                .GetField(nameof(Il2CppClassPointerStore<int>.NativeClassPtr)).GetValue(null);
        }

        private static bool IsTypeSupported(Type type)
        {
            if(type.IsValueType) return type == typeof(void);
            if(typeof(Il2CppSystem.ValueType).IsAssignableFrom(type)) return false;
            
            return typeof(Il2CppObjectBase).IsAssignableFrom(type);
        }

        private static bool IsMethodEligible(MethodInfo method)
        {
            if (method.IsGenericMethod || method.IsGenericMethodDefinition) return false;
            if (method.Name == "Finalize") return false;
            if (method.IsStatic || method.IsAbstract) return false;
            if (!IsTypeSupported(method.ReturnType))
            {
                LogSupport.Warning($"Method {method} on type {method.DeclaringType} has unsupported return type {method.ReturnType}");
                return false;
            }
            
            foreach (var parameter in method.GetParameters())
            {
                var parameterType = parameter.ParameterType;
                if (!IsTypeSupported(parameterType))
                {
                    LogSupport.Warning($"Method {method} on type {method.DeclaringType} has unsupported parameter {parameter} of type {parameterType}");
                    return false;
                }
            }

            return true;
        }
        
        private static Il2CppMethodInfo* ConvertStaticMethod(VoidCtorDelegate voidCtor, string methodName, Il2CppClass* declaringClass)
        {
            var converted = (Il2CppMethodInfo*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppMethodInfo>());
            *converted = default;
            converted->name = Marshal.StringToHGlobalAnsi(methodName);
            converted->klass = declaringClass;

            converted->invoker_method = Marshal.GetFunctionPointerForDelegate(new InvokerDelegate(StaticVoidIntPtrInvoker));
            converted->methodPointer = Marshal.GetFunctionPointerForDelegate(voidCtor);
            converted->slot = ushort.MaxValue;
            converted->return_type = (Il2CppTypeStruct*) IL2CPP.il2cpp_class_get_type(Il2CppClassPointerStore<Void>.NativeClassPtr);

            converted->flags = Il2CppMethodFlags.METHOD_ATTRIBUTE_PUBLIC |
                               Il2CppMethodFlags.METHOD_ATTRIBUTE_HIDE_BY_SIG | Il2CppMethodFlags.METHOD_ATTRIBUTE_SPECIAL_NAME | Il2CppMethodFlags.METHOD_ATTRIBUTE_RT_SPECIAL_NAME;

            return converted;
        }

        private static Il2CppMethodInfo* ConvertMethodInfo(MethodInfo monoMethod, Il2CppClass* declaringClass)
        {
            var converted = (Il2CppMethodInfo*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppMethodInfo>());
            *converted = default;
            converted->name = Marshal.StringToHGlobalAnsi(monoMethod.Name);
            converted->klass = declaringClass;

            var parameters = monoMethod.GetParameters();
            if (parameters.Length > 0)
            {
                converted->parameters_count = (byte) parameters.Length;
                var paramsArray = (Il2CppParameterInfo*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppParameterInfo>() * parameters.Length);
                converted->parameters = paramsArray;
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameterInfo = parameters[i];
                    paramsArray[i].name = Marshal.StringToHGlobalAnsi(parameterInfo.Name);
                    paramsArray[i].position = i;
                    paramsArray[i].token = 0;
                    paramsArray[i].parameter_type = (Il2CppTypeStruct*) IL2CPP.il2cpp_class_get_type(ReadClassPointerForType(parameterInfo.ParameterType));
                }
            }

            converted->invoker_method = Marshal.GetFunctionPointerForDelegate(GetOrCreateInvoker(monoMethod));
            converted->methodPointer = Marshal.GetFunctionPointerForDelegate(GetOrCreateTrampoline(monoMethod));
            converted->slot = ushort.MaxValue;
            converted->return_type = (Il2CppTypeStruct*) IL2CPP.il2cpp_class_get_type(ReadClassPointerForType(monoMethod.ReturnType));

            converted->flags = Il2CppMethodFlags.METHOD_ATTRIBUTE_PUBLIC |
                               Il2CppMethodFlags.METHOD_ATTRIBUTE_HIDE_BY_SIG;

            return converted;
        }

        private static VoidCtorDelegate CreateEmptyCtor(Type targetType)
        {
            var method = new DynamicMethod("FromIl2CppCtorDelegate", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new []{typeof(IntPtr)}, targetType, true);

            var body = method.GetILGenerator();
            
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Newobj, targetType.GetConstructor(new []{typeof(IntPtr)})!);
            body.Emit(OpCodes.Call, typeof(ClassInjector).GetMethod(nameof(ProcessNewObject))!);
            
            body.Emit(OpCodes.Ret);

            var @delegate = (VoidCtorDelegate) method.CreateDelegate(typeof(VoidCtorDelegate));
            GCHandle.Alloc(@delegate); // pin it forever
            return @delegate;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr InvokerDelegate(IntPtr methodPointer, Il2CppMethodInfo* methodInfo, IntPtr obj, IntPtr* args);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Il2CppClass* TypeToClassDelegate(Il2CppTypeStruct* type);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void VoidCtorDelegate(IntPtr objectPointer);

        public static void Finalize(IntPtr ptr)
        {
            var gcHandle = ClassInjectorBase.GetGcHandlePtrFromIl2CppObject(ptr);
            GCHandle.FromIntPtr(gcHandle).Free();
        }

        private static readonly ConcurrentDictionary<string, InvokerDelegate> InvokerCache = new ConcurrentDictionary<string, InvokerDelegate>();

        private static InvokerDelegate GetOrCreateInvoker(MethodInfo monoMethod)
        {
            return InvokerCache.GetOrAdd(ExtractSignature(monoMethod), (_, monoMethodInner) => CreateInvoker(monoMethodInner), monoMethod);
        }

        private static Delegate GetOrCreateTrampoline(MethodInfo monoMethod)
        {
            return CreateTrampoline(monoMethod);
        }
        
        private static InvokerDelegate CreateInvoker(MethodInfo monoMethod)
        {
            var parameterTypes = new[] {typeof(IntPtr), typeof(Il2CppMethodInfo*), typeof(IntPtr), typeof(IntPtr*)};

            var method = new DynamicMethod("Invoker_" + ExtractSignature(monoMethod), MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, typeof(IntPtr), parameterTypes, monoMethod.DeclaringType, true);

            var body = method.GetILGenerator();

            body.Emit(OpCodes.Ldarg_2);
            for (var i = 0; i < monoMethod.GetParameters().Length; i++)
            {
                var parameterInfo = monoMethod.GetParameters()[i];
                body.Emit(OpCodes.Ldarg_3);
                body.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                body.Emit(OpCodes.Add_Ovf_Un);
                var nativeType = parameterInfo.ParameterType.NativeType();
                body.Emit(OpCodes.Ldobj, typeof(IntPtr));
                if (nativeType != typeof(IntPtr))
                    body.Emit(OpCodes.Ldobj, nativeType);
            }

            body.Emit(OpCodes.Ldarg_0);
            body.EmitCalli(OpCodes.Calli, CallingConvention.Cdecl, monoMethod.ReturnType.NativeType(), new []{typeof(IntPtr)}.Concat(monoMethod.GetParameters().Select(it => it.ParameterType.NativeType())).ToArray());

            if (monoMethod.ReturnType == typeof(void))
            {
                body.Emit(OpCodes.Ldc_I4_0);
                body.Emit(OpCodes.Conv_I);
            }
            
            body.Emit(OpCodes.Ret);
            
            return (InvokerDelegate) method.CreateDelegate(typeof(InvokerDelegate));
        }

        private static IntPtr StaticVoidIntPtrInvoker(IntPtr methodPointer, Il2CppMethodInfo* methodInfo, IntPtr obj, IntPtr* args)
        {
            Marshal.GetDelegateForFunctionPointer<VoidCtorDelegate>(methodPointer)(obj);
            return IntPtr.Zero;
        }

        private static Delegate CreateTrampoline(MethodInfo monoMethod)
        {
            var nativeParameterTypes = new[]{typeof(IntPtr)}.Concat(monoMethod.GetParameters()
                .Select(it => it.ParameterType.NativeType()).Concat(new []{typeof(Il2CppMethodInfo*)})).ToArray();

            var managedParameters = new[] {monoMethod.DeclaringType}.Concat(monoMethod.GetParameters().Select(it => it.ParameterType)).ToArray();

            var method = new DynamicMethod("Trampoline_" + ExtractSignature(monoMethod) + monoMethod.DeclaringType + monoMethod.Name,
                MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard,
                monoMethod.ReturnType.NativeType(), nativeParameterTypes,
                monoMethod.DeclaringType, true);

            var signature = new DelegateSupport.MethodSignature(monoMethod, true);
            var delegateType = DelegateSupport.GetOrCreateDelegateType(signature, monoMethod);
            
            var body = method.GetILGenerator();

            body.BeginExceptionBlock();
            
            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Call, typeof(ClassInjectorBase).GetMethod(nameof(ClassInjectorBase.GetMonoObjectFromIl2CppPointer))!);
            body.Emit(OpCodes.Castclass, monoMethod.DeclaringType);
            
            for (var i = 1; i < managedParameters.Length; i++)
            {
                body.Emit(OpCodes.Ldarg, i);
                var parameter = managedParameters[i];
                if (!parameter.IsValueType)
                {
                    if(parameter == typeof(string))
                        body.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.Il2CppStringToManaged))!);
                    else
                        body.Emit(OpCodes.Newobj, parameter.GetConstructor(new []{typeof(IntPtr)})!);
                }
            }
            
            body.Emit(OpCodes.Call, monoMethod);
            if (monoMethod.ReturnType == typeof(void))
            {
                // do nothing
            } else if (monoMethod.ReturnType == typeof(string))
            {
                body.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.ManagedStringToIl2Cpp))!);
            } else if (monoMethod.ReturnType.IsValueType)
            {
                throw new NotImplementedException("Value types are not supported for returns");
            }
            else
            {
                body.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.Il2CppObjectBaseToPtr))!);
            }
            body.Emit(OpCodes.Ret);
            
            var exceptionLocal = body.DeclareLocal(typeof(Exception));
            body.BeginCatchBlock(typeof(Exception));
            body.Emit(OpCodes.Stloc, exceptionLocal);
            body.Emit(OpCodes.Ldstr, "Exception in IL2CPP-to-Managed trampoline, not passing it to il2cpp: ");
            body.Emit(OpCodes.Ldloc, exceptionLocal);
            body.Emit(OpCodes.Callvirt, typeof(object).GetMethod(nameof(ToString))!);
            body.Emit(OpCodes.Call, typeof(string).GetMethod(nameof(string.Concat), new []{typeof(string), typeof(string)})!);
            body.Emit(OpCodes.Call, typeof(LogSupport).GetMethod(nameof(LogSupport.Error))!);
            
            body.EndExceptionBlock();
            
            if (monoMethod.ReturnType != typeof(void))
            {
                body.Emit(OpCodes.Ldc_I4_0);
                body.Emit(OpCodes.Conv_I);
            }
            body.Emit(OpCodes.Ret);

            var @delegate = method.CreateDelegate(delegateType);
            GCHandle.Alloc(@delegate); // pin it forever
            return @delegate;
        }

        private static string ExtractSignature(MethodInfo monoMethod)
        {
            var builder = new StringBuilder();
            builder.Append(monoMethod.ReturnType.NativeType().Name);
            builder.Append(monoMethod.IsStatic ? "" : "This");
            foreach (var parameterInfo in monoMethod.GetParameters())
                builder.Append(parameterInfo.ParameterType.NativeType().Name);
            return builder.ToString();
        }

        private static Type NativeType(this Type type)
        {
            return type.IsValueType ? type : typeof(IntPtr);
        }

        private static void HookClassFromType()
        {
            LogSupport.Info("xref scanning type to class");
            var lib = LoadLibrary("GameAssembly.dll");
            LogSupport.Info($"lib: {lib}");
            var classFromTypeEntryPoint = GetProcAddress(lib, nameof(IL2CPP.il2cpp_class_from_il2cpp_type));
            LogSupport.Info($"hook_method: {classFromTypeEntryPoint}");


            var scanner = new XrefScanner(classFromTypeEntryPoint);
            var targetMethod = scanner.JumpTargets().Single();
            LogSupport.Info($"target_method: {targetMethod}");

            if (targetMethod == IntPtr.Zero)
                return;

            IntPtr* targetVarPointer = &targetMethod;
            DoHook((IntPtr) targetVarPointer,
                Marshal.GetFunctionPointerForDelegate(new TypeToClassDelegate(ClassFromTypePatch)));
            ourOriginalTypeToClassMethod = Marshal.GetDelegateForFunctionPointer<TypeToClassDelegate>(targetMethod);
            
            LogSupport.Info("patched");
        }

        public static Action<IntPtr, IntPtr> DoHook;

        private static long ourClassOverrideCounter = -2;
        private static readonly ConcurrentDictionary<long, IntPtr> FakeTokenClasses = new ConcurrentDictionary<long, IntPtr>();

        private static TypeToClassDelegate ourOriginalTypeToClassMethod;
        private static readonly VoidCtorDelegate FinalizeDelegate = Finalize;

        private static Il2CppClass* ClassFromTypePatch(Il2CppTypeStruct* type)
        {
            if ((long) type->data < 0 && (type->type == Il2CppTypeEnum.IL2CPP_TYPE_CLASS || type->type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE))
            {
                FakeTokenClasses.TryGetValue((long) type->data, out var classPointer);
                return (Il2CppClass*) classPointer;
            }
            return ourOriginalTypeToClassMethod(type);
        }

        [DllImport("kernel32", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        
        [DllImport("kernel32", SetLastError=true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);
    }
}