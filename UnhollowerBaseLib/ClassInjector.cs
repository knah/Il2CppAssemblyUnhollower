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
using UnhollowerBaseLib.Attributes;
using UnhollowerBaseLib.Runtime;
using UnhollowerBaseLib.Runtime.VersionSpecific.Assembly;
using UnhollowerBaseLib.Runtime.VersionSpecific.Class;
using UnhollowerBaseLib.Runtime.VersionSpecific.Image;
using UnhollowerRuntimeLib.XrefScans;
using Void = Il2CppSystem.Void;

namespace UnhollowerRuntimeLib
{
    public unsafe static class ClassInjector
    {
        private static INativeAssemblyStruct FakeAssembly;
        private static INativeImageStruct FakeImage;

        /// <summary> type.FullName </summary>
        private static readonly HashSet<string> InjectedTypes = new HashSet<string>();
        /// <summary> (namespace, class, image) : pointer </summary>
        private static readonly Dictionary<(string, string, IntPtr), IntPtr> ClassFromNameDictionary = new Dictionary<(string, string, IntPtr), IntPtr>();
        /// <summary> (method) : (method_inst, method) </summary>
        private static readonly Dictionary<IntPtr, (MethodInfo, Dictionary<IntPtr, IntPtr>)> InflatedMethodFromContextDictionary = new Dictionary<IntPtr, (MethodInfo, Dictionary<IntPtr, IntPtr>)>();

        static void CreateFakeAssembly()
        {
            FakeAssembly = UnityVersionHandler.NewAssembly();
            FakeImage = UnityVersionHandler.NewImage();

            FakeAssembly.Name = Marshal.StringToHGlobalAnsi("InjectedMonoTypes");

            FakeImage.Assembly = FakeAssembly.AssemblyPointer;
            FakeImage.Dynamic = 1;
            FakeImage.Name = FakeAssembly.Name;
            if (FakeImage.HasNameNoExt)
                FakeImage.NameNoExt = FakeImage.Name;
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
            var objectKlass = (Il2CppClass*)IL2CPP.il2cpp_object_get_class(pointer);
            var targetGcHandlePointer = IntPtr.Add(pointer, (int)UnityVersionHandler.Wrap(objectKlass).InstanceSize - IntPtr.Size);
            *(IntPtr*)targetGcHandlePointer = handleAsPointer;
        }

        public static void RegisterTypeInIl2Cpp<T>() where T : class => RegisterTypeInIl2CppImpl(typeof(T), true, Array.Empty<INativeClassStruct>());
        public static void RegisterTypeInIl2Cpp<T>(bool logSuccess) where T : class => RegisterTypeInIl2CppImpl(typeof(T), logSuccess, Array.Empty<INativeClassStruct>());
        public static void RegisterTypeInIl2Cpp(Type type, bool logSuccess) => RegisterTypeInIl2CppImpl(type, logSuccess, Array.Empty<INativeClassStruct>());
        public static void RegisterTypeInIl2CppWithInterfaces<T>(params Type[] interfaces) where T : class => RegisterTypeInIl2CppWithInterfaces(typeof(T), true, interfaces);
        public static void RegisterTypeInIl2CppWithInterfaces<T>(bool logSuccess, params Type[] interfaces) where T : class => RegisterTypeInIl2CppWithInterfaces(typeof(T), logSuccess, interfaces);
        public static void RegisterTypeInIl2CppWithInterfaces(Type type, bool logSuccess, params Type[] interfaces)
        {
            RegisterTypeInIl2CppImpl(type, logSuccess, interfaces.Select(it =>
            {
                var classPointer = ReadClassPointerForType(it);
                if (classPointer == IntPtr.Zero)
                    throw new ArgumentException($"Type {it} doesn't have an IL2CPP class pointer, which means it's not an IL2CPP interface");
                return UnityVersionHandler.Wrap((Il2CppClass*)classPointer);
            }).ToArray());
        }
        
        public static void RegisterTypeInIl2CppImpl(Type type, bool logSuccess, params INativeClassStruct[] interfaces)
        {
            if(type == null)
                throw new ArgumentException($"Type argument cannot be null");

            if (type.IsGenericType || type.IsGenericTypeDefinition)
                throw new ArgumentException($"Type {type} is generic and can't be used in il2cpp");

            var currentPointer = ReadClassPointerForType(type);
            if (currentPointer != IntPtr.Zero)
                return;//already registered in il2cpp

            var baseType = type.BaseType;
            if (baseType == null)
                throw new ArgumentException($"Class {type} does not inherit from a class registered in il2cpp");

            var baseClassPointer = UnityVersionHandler.Wrap((Il2CppClass*)ReadClassPointerForType(baseType));
            if (baseClassPointer == null)
            {
                RegisterTypeInIl2Cpp(baseType, logSuccess);
                baseClassPointer = UnityVersionHandler.Wrap((Il2CppClass*)ReadClassPointerForType(baseType));
            }

            if (baseClassPointer.ValueType || baseClassPointer.EnumType)
                throw new ArgumentException($"Base class {baseType} is value type and can't be inherited from");

            if (baseClassPointer.IsGeneric)
                throw new ArgumentException($"Base class {baseType} is generic and can't be inherited from");

            if ((baseClassPointer.Flags & Il2CppClassAttributes.TYPE_ATTRIBUTE_SEALED) != 0)
                throw new ArgumentException($"Base class {baseType} is sealed and can't be inherited from");

            if ((baseClassPointer.Flags & Il2CppClassAttributes.TYPE_ATTRIBUTE_INTERFACE) != 0)
                throw new ArgumentException($"Base class {baseType} is an interface and can't be inherited from");

            if (interfaces.Any(i => (i.Flags & Il2CppClassAttributes.TYPE_ATTRIBUTE_INTERFACE) == 0))
                throw new ArgumentException($"Interfaces {interfaces} are not interfaces");

            lock (InjectedTypes)
                if (!InjectedTypes.Add(type.FullName))
                    throw new ArgumentException($"Type with FullName {type.FullName} is already injected. Don't inject the same type twice, or use a different namespace");

            if (ourOriginalGenericGetMethod == null) HookGenericMethodGetMethod();
            if (ourOriginalTypeToClassMethod == null) HookClassFromType();
            if (originalClassFromNameMethod == null) HookClassFromName();
            if (FakeAssembly == null) CreateFakeAssembly();

            var interfaceFunctionCount = interfaces.Sum(i => i.MethodCount);
            var classPointer = UnityVersionHandler.NewClass(baseClassPointer.VtableCount + interfaceFunctionCount);

            classPointer.Image = FakeImage.ImagePointer;
            classPointer.Parent = baseClassPointer.ClassPointer;
            classPointer.ElementClass = classPointer.Class = classPointer.CastClass = classPointer.ClassPointer;
            classPointer.NativeSize = -1;
            classPointer.ActualSize = classPointer.InstanceSize = baseClassPointer.InstanceSize + (uint)IntPtr.Size;

            classPointer.Initialized = true;
            classPointer.InitializedAndNoError = true;
            classPointer.SizeInited = true;
            classPointer.HasFinalize = true;
            classPointer.IsVtableInitialized = true;

            classPointer.Name = Marshal.StringToHGlobalAnsi(type.Name);
            classPointer.Namespace = Marshal.StringToHGlobalAnsi(type.Namespace);

            classPointer.ThisArg.Type = classPointer.ByValArg.Type = Il2CppTypeEnum.IL2CPP_TYPE_CLASS;
            classPointer.ThisArg.ByRef = true;

            classPointer.Flags = baseClassPointer.Flags; // todo: adjust flags?

            var eligibleMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(IsMethodEligible).ToArray();
            var methodCount = 2 + eligibleMethods.Length; // 1 is the finalizer, 1 is empty ctor

            classPointer.MethodCount = (ushort)methodCount;
            var methodPointerArray = (Il2CppMethodInfo**)Marshal.AllocHGlobal(methodCount * IntPtr.Size);
            classPointer.Methods = methodPointerArray;

            methodPointerArray[0] = ConvertStaticMethod(FinalizeDelegate, "Finalize", classPointer);
            var finalizeMethod = UnityVersionHandler.Wrap(methodPointerArray[0]);
            methodPointerArray[1] = ConvertStaticMethod(CreateEmptyCtor(type), ".ctor", classPointer);
            Dictionary<(string name, int paramCount, bool isGeneric), int> infos = new Dictionary<(string, int, bool), int>(eligibleMethods.Length);
            for (var i = 0; i < eligibleMethods.Length; i++)
            {
                var methodInfo = eligibleMethods[i];
                var methodInfoPointer = methodPointerArray[i + 2] = ConvertMethodInfo(methodInfo, classPointer);
                if (methodInfo.IsGenericMethod)
                    InflatedMethodFromContextDictionary.Add((IntPtr)methodInfoPointer, (methodInfo, new Dictionary<IntPtr, IntPtr>()));
                var methodName = methodInfo.Name;
                infos[(methodInfo.Name, methodInfo.GetParameters().Length, methodInfo.IsGenericMethod)] = i + 2;
            }

            var vTablePointer = (VirtualInvokeData*)classPointer.VTable;
            var baseVTablePointer = (VirtualInvokeData*)baseClassPointer.VTable;
            classPointer.VtableCount = (ushort)(baseClassPointer.VtableCount + interfaceFunctionCount);
            for (var i = 0; i < baseClassPointer.VtableCount; i++)
            {
                vTablePointer[i] = baseVTablePointer[i];
                var vTableMethod = UnityVersionHandler.Wrap(vTablePointer[i].method);
                if (Marshal.PtrToStringAnsi(vTableMethod.Name) == "Finalize") // slot number is not static
                {
                    vTablePointer[i].method = methodPointerArray[0];
                    vTablePointer[i].methodPtr = finalizeMethod.MethodPointer;
                }
            }

            var offsets = new int[interfaces.Length];

            var index = baseClassPointer.VtableCount;
            for (var i = 0; i < interfaces.Length; i++)
            {
                offsets[i] = index;
                for (var j = 0; j < interfaces[i].MethodCount; j++)
                {
                    var vTableMethod = UnityVersionHandler.Wrap(interfaces[i].Methods[j]);
                    var methodName = Marshal.PtrToStringAnsi(vTableMethod.Name);
                    if (!infos.TryGetValue((methodName, vTableMethod.ParametersCount, vTableMethod.IsGeneric), out var methodIndex))
                    {
                        ++index;
                        continue;
                    }
                    var method = methodPointerArray[methodIndex];
                    vTablePointer[index].method = method;
                    vTablePointer[index].methodPtr = UnityVersionHandler.Wrap(method).MethodPointer;
                    ++index;
                }
            }

            var interfaceCount = baseClassPointer.InterfaceCount + interfaces.Length;
            classPointer.InterfaceCount = (ushort)interfaceCount;
            classPointer.ImplementedInterfaces = (Il2CppClass**)Marshal.AllocHGlobal(interfaceCount * IntPtr.Size);
            for (int i = 0; i < baseClassPointer.InterfaceCount; i++)
                classPointer.ImplementedInterfaces[i] = baseClassPointer.ImplementedInterfaces[i];
            for (int i = baseClassPointer.InterfaceCount; i < interfaceCount; i++)
                classPointer.ImplementedInterfaces[i] = interfaces[i - baseClassPointer.InterfaceCount].ClassPointer;

            var interfaceOffsetsCount = baseClassPointer.InterfaceOffsetsCount + interfaces.Length;
            classPointer.InterfaceOffsetsCount = (ushort)interfaceOffsetsCount;
            classPointer.InterfaceOffsets = (Il2CppRuntimeInterfaceOffsetPair*)Marshal.AllocHGlobal(interfaceOffsetsCount * Marshal.SizeOf<Il2CppRuntimeInterfaceOffsetPair>());
            for (int i = 0; i < baseClassPointer.InterfaceOffsetsCount; i++)
                classPointer.InterfaceOffsets[i] = baseClassPointer.InterfaceOffsets[i];
            for (int i = baseClassPointer.InterfaceOffsetsCount; i < interfaceOffsetsCount; i++)
                classPointer.InterfaceOffsets[i] = new Il2CppRuntimeInterfaceOffsetPair {
                    interfaceType = interfaces[i - baseClassPointer.InterfaceOffsetsCount].ClassPointer,
                    offset = offsets[i - baseClassPointer.InterfaceOffsetsCount]
                };

            var TypeHierarchyDepth = 1 + baseClassPointer.TypeHierarchyDepth;
            classPointer.TypeHierarchyDepth = (byte)TypeHierarchyDepth;
            classPointer.TypeHierarchy = (Il2CppClass**)Marshal.AllocHGlobal(TypeHierarchyDepth * IntPtr.Size);
            for (var i = 0; i < TypeHierarchyDepth; i++)
                classPointer.TypeHierarchy[i] = baseClassPointer.TypeHierarchy[i];
            classPointer.TypeHierarchy[TypeHierarchyDepth - 1] = classPointer.ClassPointer;

            var newCounter = Interlocked.Decrement(ref ourClassOverrideCounter);
            FakeTokenClasses[newCounter] = classPointer.Pointer;
            classPointer.ByValArg.Data = classPointer.ThisArg.Data = (IntPtr)newCounter;

            RuntimeSpecificsStore.SetClassInfo(classPointer.Pointer, true, true);
            WriteClassPointerForType(type, classPointer.Pointer);

            AddToClassFromNameDictionary(type, classPointer.Pointer);

            if (logSuccess) LogSupport.Info($"Registered mono type {type} in il2cpp domain");
        }

        private static void AddToClassFromNameDictionary<T>(IntPtr typePointer) where T : class => AddToClassFromNameDictionary(typeof(T), typePointer);
        private static void AddToClassFromNameDictionary(Type type, IntPtr typePointer)
        {
            string klass = type.Name;
            if (klass == null) return;
            string namespaze = type.Namespace ?? string.Empty;
            var attribute = Attribute.GetCustomAttribute(type, typeof(UnhollowerBaseLib.Attributes.ClassInjectionAssemblyTargetAttribute)) as UnhollowerBaseLib.Attributes.ClassInjectionAssemblyTargetAttribute;

            foreach (IntPtr image in ((attribute is null) ? IL2CPP.GetIl2CppImages() : attribute.GetImagePointers()) )
            {
                ClassFromNameDictionary.Add((namespaze, klass, image), typePointer);
            }
        }

        internal static IntPtr ReadClassPointerForType(Type type)
        {
            if (type == typeof(void)) return Il2CppClassPointerStore<Void>.NativeClassPtr;
            return (IntPtr)typeof(Il2CppClassPointerStore<>).MakeGenericType(type)
                .GetField(nameof(Il2CppClassPointerStore<int>.NativeClassPtr)).GetValue(null);
        }

        internal static void WriteClassPointerForType(Type type, IntPtr value)
        {
            typeof(Il2CppClassPointerStore<>).MakeGenericType(type)
                .GetField(nameof(Il2CppClassPointerStore<int>.NativeClassPtr)).SetValue(null, value);
        }

        private static bool IsTypeSupported(Type type)
        {
            if (type.IsValueType ||
                type == typeof(string) ||
                type.IsGenericParameter) return true;
            if (typeof(Il2CppSystem.ValueType).IsAssignableFrom(type)) return false;

            return typeof(Il2CppObjectBase).IsAssignableFrom(type);
        }

        private static bool IsMethodEligible(MethodInfo method)
        {
            if (method.Name == "Finalize") return false;
            if (method.IsStatic || method.IsAbstract) return false;
            if (method.CustomAttributes.Any(it => it.AttributeType == typeof(HideFromIl2CppAttribute))) return false;

            if (
                method.DeclaringType != null &&
                method.DeclaringType.GetProperties()
                    .Where(property => property.GetAccessors(true).Contains(method))
                    .Any(property => property.CustomAttributes.Any(it => it.AttributeType == typeof(HideFromIl2CppAttribute)))
            )
            {
                return false;
            }

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

        private static Il2CppMethodInfo* ConvertStaticMethod(VoidCtorDelegate voidCtor, string methodName, INativeClassStruct declaringClass)
        {
            var converted = UnityVersionHandler.NewMethod();
            converted.Name = Marshal.StringToHGlobalAnsi(methodName);
            converted.Class = declaringClass.ClassPointer;

            converted.InvokerMethod = Marshal.GetFunctionPointerForDelegate(new InvokerDelegate(StaticVoidIntPtrInvoker));
            converted.MethodPointer = Marshal.GetFunctionPointerForDelegate(voidCtor);
            converted.Slot = ushort.MaxValue;
            converted.ReturnType = (Il2CppTypeStruct*)IL2CPP.il2cpp_class_get_type(Il2CppClassPointerStore<Void>.NativeClassPtr);

            converted.Flags = Il2CppMethodFlags.METHOD_ATTRIBUTE_PUBLIC |
                               Il2CppMethodFlags.METHOD_ATTRIBUTE_HIDE_BY_SIG | Il2CppMethodFlags.METHOD_ATTRIBUTE_SPECIAL_NAME | Il2CppMethodFlags.METHOD_ATTRIBUTE_RT_SPECIAL_NAME;

            return converted.MethodInfoPointer;
        }

        private static Il2CppMethodInfo* ConvertMethodInfo(MethodInfo monoMethod, INativeClassStruct declaringClass)
        {
            var converted = UnityVersionHandler.NewMethod();
            converted.Name = Marshal.StringToHGlobalAnsi(monoMethod.Name);
            converted.Class = declaringClass.ClassPointer;

            var parameters = monoMethod.GetParameters();
            if (parameters.Length > 0)
            {
                converted.ParametersCount = (byte)parameters.Length;
                var paramsArray = UnityVersionHandler.NewMethodParameterArray(parameters.Length);
                converted.Parameters = paramsArray[0];
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameterInfo = parameters[i];
                    var param = UnityVersionHandler.Wrap(paramsArray[i]);
                    if (UnityVersionHandler.ParameterInfoHasNamePosToken())
                    {
                        param.Name = Marshal.StringToHGlobalAnsi(parameterInfo.Name);
                        param.Position = i;
                        param.Token = 0;
                    }
                    var parameterType = parameterInfo.ParameterType;
                    if (!parameterType.IsGenericParameter)
                        param.ParameterType = (Il2CppTypeStruct*)IL2CPP.il2cpp_class_get_type(ReadClassPointerForType(parameterType));
                    else
                    {
                        var type = UnityVersionHandler.NewType();
                        type.Type = Il2CppTypeEnum.IL2CPP_TYPE_MVAR;
                        param.ParameterType = type.TypePointer;
                    }
                }
            }

            if (monoMethod.IsGenericMethod)
            {
                if (monoMethod.ContainsGenericParameters)
                    converted.IsGeneric = true;
                else
                    converted.IsInflated = true;
            }

            if (!monoMethod.ContainsGenericParameters)
            {
                converted.InvokerMethod = Marshal.GetFunctionPointerForDelegate(GetOrCreateInvoker(monoMethod));
                converted.MethodPointer = Marshal.GetFunctionPointerForDelegate(GetOrCreateTrampoline(monoMethod));
            }
            converted.Slot = ushort.MaxValue;

            if (!monoMethod.ReturnType.IsGenericParameter)
                converted.ReturnType = (Il2CppTypeStruct*)IL2CPP.il2cpp_class_get_type(ReadClassPointerForType(monoMethod.ReturnType));
            else
            {
                var type = UnityVersionHandler.NewType();
                type.Type = Il2CppTypeEnum.IL2CPP_TYPE_MVAR;
                converted.ReturnType = type.TypePointer;
            }

            converted.Flags = Il2CppMethodFlags.METHOD_ATTRIBUTE_PUBLIC |
                               Il2CppMethodFlags.METHOD_ATTRIBUTE_HIDE_BY_SIG;

            return converted.MethodInfoPointer;
        }

        private static VoidCtorDelegate CreateEmptyCtor(Type targetType)
        {
            var method = new DynamicMethod("FromIl2CppCtorDelegate", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new[] { typeof(IntPtr) }, targetType, true);

            var body = method.GetILGenerator();

            body.Emit(OpCodes.Ldarg_0);
            body.Emit(OpCodes.Newobj, targetType.GetConstructor(new[] { typeof(IntPtr) })!);
            body.Emit(OpCodes.Call, typeof(ClassInjector).GetMethod(nameof(ProcessNewObject))!);

            body.Emit(OpCodes.Ret);

            var @delegate = (VoidCtorDelegate)method.CreateDelegate(typeof(VoidCtorDelegate));
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
            var parameterTypes = new[] { typeof(IntPtr), typeof(Il2CppMethodInfo*), typeof(IntPtr), typeof(IntPtr*) };

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
            body.EmitCalli(OpCodes.Calli, CallingConvention.Cdecl, monoMethod.ReturnType.NativeType(), new[] { typeof(IntPtr) }.Concat(monoMethod.GetParameters().Select(it => it.ParameterType.NativeType())).ToArray());

            if (monoMethod.ReturnType == typeof(void))
            {
                body.Emit(OpCodes.Ldc_I4_0);
                body.Emit(OpCodes.Conv_I);
            }
            else if (monoMethod.ReturnType.IsValueType)
            {
                var returnValue = body.DeclareLocal(monoMethod.ReturnType);
                body.Emit(OpCodes.Stloc, returnValue);
                var classField = typeof(Il2CppClassPointerStore<>).MakeGenericType(monoMethod.ReturnType)
                                                                  .GetField(nameof(Il2CppClassPointerStore<int>.NativeClassPtr));
                body.Emit(OpCodes.Ldsfld, classField);
                body.Emit(OpCodes.Ldloca, returnValue);
                body.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.il2cpp_value_box))!);
            }

            body.Emit(OpCodes.Ret);

            return (InvokerDelegate)method.CreateDelegate(typeof(InvokerDelegate));
        }

        private static IntPtr StaticVoidIntPtrInvoker(IntPtr methodPointer, Il2CppMethodInfo* methodInfo, IntPtr obj, IntPtr* args)
        {
            Marshal.GetDelegateForFunctionPointer<VoidCtorDelegate>(methodPointer)(obj);
            return IntPtr.Zero;
        }

        private static Delegate CreateTrampoline(MethodInfo monoMethod)
        {
            var nativeParameterTypes = new[] { typeof(IntPtr) }.Concat(monoMethod.GetParameters()
                .Select(it => it.ParameterType.NativeType()).Concat(new[] { typeof(Il2CppMethodInfo*) })).ToArray();

            var managedParameters = new[] { monoMethod.DeclaringType }.Concat(monoMethod.GetParameters().Select(it => it.ParameterType)).ToArray();

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
                    if (parameter == typeof(string))
                        body.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.Il2CppStringToManaged))!);
                    else
                    {
                        var labelNull = body.DefineLabel();
                        var labelNotNull = body.DefineLabel();
                        body.Emit(OpCodes.Dup);
                        body.Emit(OpCodes.Brfalse, labelNull);
                        body.Emit(OpCodes.Newobj, parameter.GetConstructor(new[] { typeof(IntPtr) })!);
                        body.Emit(OpCodes.Br, labelNotNull);
                        body.MarkLabel(labelNull);
                        body.Emit(OpCodes.Pop);
                        body.Emit(OpCodes.Ldnull);
                        body.MarkLabel(labelNotNull);
                    }
                }
            }

            body.Emit(OpCodes.Call, monoMethod);
            if (monoMethod.ReturnType == typeof(void))
            {
                // do nothing
            }
            else if (monoMethod.ReturnType == typeof(string))
            {
                body.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.ManagedStringToIl2Cpp))!);
            }
            else if (!monoMethod.ReturnType.IsValueType)
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
            body.Emit(OpCodes.Call, typeof(string).GetMethod(nameof(string.Concat), new[] { typeof(string), typeof(string) })!);
            body.Emit(OpCodes.Call, typeof(LogSupport).GetMethod(nameof(LogSupport.Error))!);

            body.EndExceptionBlock();

            if (monoMethod.ReturnType != typeof(void))
            {
                if (monoMethod.ReturnType.IsValueType)
                {
                    if(monoMethod.ReturnType.IsPrimitive)
                    { 
                        if(monoMethod.ReturnType == typeof(float))
                            body.Emit(OpCodes.Ldc_R4, 0);
                        else if (monoMethod.ReturnType == typeof(double))
                            body.Emit(OpCodes.Ldc_R8, 0);
                        else
                        {
                            body.Emit(OpCodes.Ldc_I4_0);
                            if(monoMethod.ReturnType == typeof(long) || monoMethod.ReturnType == typeof(ulong))
                            {
                                body.Emit(OpCodes.Conv_I8);
                            }
                        }
                    }
                    else
                    {
                        var local = body.DeclareLocal(monoMethod.ReturnType);

                        body.Emit(OpCodes.Ldloca_S, local);
                        body.Emit(OpCodes.Initobj, monoMethod.ReturnType);
                        body.Emit(OpCodes.Ldloc_S, local);
                    }
                } else {
                    body.Emit(OpCodes.Ldc_I4_0);
                    body.Emit(OpCodes.Conv_I);
                }
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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate Il2CppMethodInfo* GenericGetMethodDelegate(Il2CppGenericMethod* gmethod, bool copyMethodPtr);
        private static volatile GenericGetMethodDelegate ourOriginalGenericGetMethod;

        private static void HookGenericMethodGetMethod()
        {
            var lib = LoadLibrary("GameAssembly.dll");
            var getVirtualMethodEntryPoint = GetProcAddress(lib, nameof(IL2CPP.il2cpp_object_get_virtual_method));
            LogSupport.Trace($"il2cpp_object_get_virtual_method entry address: {getVirtualMethodEntryPoint}");

            var getVirtualMethodMethod = XrefScannerLowLevel.JumpTargets(getVirtualMethodEntryPoint).Single();
            LogSupport.Trace($"Xref scan target 1: {getVirtualMethodMethod}");

            var targetMethod = XrefScannerLowLevel.JumpTargets(getVirtualMethodMethod).Last();
            LogSupport.Trace($"Xref scan target 2: {targetMethod}");

            if (targetMethod == IntPtr.Zero)
                return;

            var targetTargets = XrefScannerLowLevel.JumpTargets(targetMethod).Take(2).ToList();
            if (targetTargets.Count == 1) // U2021.2.0+, there's additional shim that takes 3 parameters
                targetMethod = targetTargets[0];

            ourOriginalGenericGetMethod = Detour.Detour(targetMethod, new GenericGetMethodDelegate(GenericGetMethodPatch));
            LogSupport.Trace("il2cpp_class_from_il2cpp_type patched");
        }

        private static System.Type SystemTypeFromIl2CppType(Il2CppTypeStruct *typePointer)
        {
            var klass = UnityVersionHandler.Wrap(ClassFromTypePatch(typePointer));
            var fullName = Marshal.PtrToStringAnsi(klass.Namespace) + "." + Marshal.PtrToStringAnsi(klass.Name);
            if (fullName == "System.String")
                return typeof(string);

            var type = Type.GetType(fullName);
            if (type.IsValueType)
                return type;

            if (fullName.StartsWith("System"))
                fullName = "Il2Cpp" + fullName;
            var systemType = AppDomain.CurrentDomain.GetAssemblies()
                                .SelectMany(a => a.GetTypes())
                                .First(t => t.FullName == fullName);
            return systemType;
        }

        private static Il2CppMethodInfo* GenericGetMethodPatch(Il2CppGenericMethod* gmethod, bool copyMethodPtr)
        {
            if (InflatedMethodFromContextDictionary.TryGetValue((IntPtr)gmethod->methodDefinition, out var methods))
            {
                var instancePointer = gmethod->context.method_inst;
                if (methods.Item2.TryGetValue((IntPtr)instancePointer, out var inflatedMethodPointer))
                    return (Il2CppMethodInfo*)inflatedMethodPointer;

                var typeArguments = new Type[instancePointer->type_argc];
                for (var i = 0; i < instancePointer->type_argc; i++)
                    typeArguments[i] = SystemTypeFromIl2CppType(instancePointer->type_argv[i]);
                var inflatedMethod = methods.Item1.MakeGenericMethod(typeArguments);
                LogSupport.Trace("Inflated method: " + inflatedMethod.Name);
                inflatedMethodPointer = (IntPtr)ConvertMethodInfo(inflatedMethod, UnityVersionHandler.Wrap(UnityVersionHandler.Wrap(gmethod->methodDefinition).Class));
                methods.Item2.Add((IntPtr)instancePointer, inflatedMethodPointer);

                return (Il2CppMethodInfo*)inflatedMethodPointer;
            }
            return ourOriginalGenericGetMethod(gmethod, copyMethodPtr);
        }

        private static void HookClassFromType()
        {
            var lib = LoadLibrary("GameAssembly.dll");
            var classFromTypeEntryPoint = GetProcAddress(lib, nameof(IL2CPP.il2cpp_class_from_il2cpp_type));
            LogSupport.Trace($"il2cpp_class_from_il2cpp_type entry address: {classFromTypeEntryPoint}");

            var targetMethod = XrefScannerLowLevel.JumpTargets(classFromTypeEntryPoint).Single();
            LogSupport.Trace($"Xref scan target: {targetMethod}");

            if (targetMethod == IntPtr.Zero)
                return;

            ourOriginalTypeToClassMethod = Detour.Detour(targetMethod, new TypeToClassDelegate(ClassFromTypePatch));
            LogSupport.Trace("il2cpp_class_from_il2cpp_type patched");
        }


        public static IManagedDetour Detour = new DoHookDetour();
        [Obsolete("Set Detour instead")]
        public static Action<IntPtr, IntPtr> DoHook;

        private static long ourClassOverrideCounter = -2;
        private static readonly ConcurrentDictionary<long, IntPtr> FakeTokenClasses = new ConcurrentDictionary<long, IntPtr>();

        private static volatile TypeToClassDelegate ourOriginalTypeToClassMethod;
        private static readonly VoidCtorDelegate FinalizeDelegate = Finalize;

        private static Il2CppClass* ClassFromTypePatch(Il2CppTypeStruct* type)
        {
            var wrappedType = UnityVersionHandler.Wrap(type);
            if ((long)wrappedType.Data < 0 && (wrappedType.Type == Il2CppTypeEnum.IL2CPP_TYPE_CLASS || wrappedType.Type == Il2CppTypeEnum.IL2CPP_TYPE_VALUETYPE))
            {
                FakeTokenClasses.TryGetValue((long)wrappedType.Data, out var classPointer);
                return (Il2CppClass*)classPointer;
            }
            // possible race: other threads can try resolving classes after the hook is installed but before delegate field is set
            while (ourOriginalTypeToClassMethod == null) Thread.Sleep(1);
            return ourOriginalTypeToClassMethod(type);
        }

        #region Class From Name Patch
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr ClassFromNameDelegate(IntPtr intPtr, IntPtr str1, IntPtr str2);

        private static ClassFromNameDelegate originalClassFromNameMethod;
        private static readonly ClassFromNameDelegate hookedClassFromName = new ClassFromNameDelegate(ClassFromNamePatch);

        private static void HookClassFromName()
        {
            var lib = LoadLibrary("GameAssembly.dll");
            var classFromNameEntryPoint = GetProcAddress(lib, nameof(IL2CPP.il2cpp_class_from_name));
            LogSupport.Trace($"il2cpp_class_from_name entry address: {classFromNameEntryPoint}");

            if (classFromNameEntryPoint == IntPtr.Zero) return;

            originalClassFromNameMethod = Detour.Detour(classFromNameEntryPoint, hookedClassFromName);
            LogSupport.Trace("il2cpp_class_from_name patched");
        }

        private static IntPtr ClassFromNamePatch(IntPtr param1, IntPtr param2, IntPtr param3)
        {
            try
            {
                // possible race: other threads can try resolving classes after the hook is installed but before delegate field is set
                while (originalClassFromNameMethod == null) Thread.Sleep(1);
                IntPtr intPtr = originalClassFromNameMethod.Invoke(param1, param2, param3);

                if (intPtr == IntPtr.Zero)
                {
                    string namespaze = Marshal.PtrToStringAnsi(param2);
                    string klass = Marshal.PtrToStringAnsi(param3);
                    ClassFromNameDictionary.TryGetValue((namespaze, klass, param1),out intPtr);
                }

                return intPtr;
            }
            catch (Exception e)
            {
                LogSupport.Error(e.Message);
                return IntPtr.Zero;
            }
        }
        #endregion

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        private class DoHookDetour : IManagedDetour
        {
            // In some cases garbage collection of delegates can release their native function pointer too - keep all of them alive to avoid that
            // ReSharper disable once CollectionNeverQueried.Local
            private static readonly List<object> PinnedDelegates = new List<object>();

            public T Detour<T>(IntPtr @from, T to) where T : Delegate
            {
                IntPtr* targetVarPointer = &from;
                PinnedDelegates.Add(to);
                DoHook((IntPtr)targetVarPointer, Marshal.GetFunctionPointerForDelegate(to));
                return Marshal.GetDelegateForFunctionPointer<T>(from);
            }
        }
    }

    public interface IManagedDetour
    {
        /// <summary>
        /// Patch the native function at address specified in `from`, replacing it with `to`, and return a delegate to call the original native function
        /// </summary>
        T Detour<T>(IntPtr from, T to) where T : Delegate;
    }
}