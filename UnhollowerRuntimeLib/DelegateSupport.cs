using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using UnhollowerBaseLib;

namespace UnhollowerRuntimeLib
{
    public static unsafe class DelegateSupport
    {
        private static IntPtr ourCounter = new IntPtr(1);
        private static readonly Dictionary<IntPtr, Delegate> ourDelegates = new Dictionary<IntPtr, Delegate>();

        public static Delegate GetDelegate(IntPtr counter) => ourDelegates[counter];

        private static readonly ConcurrentDictionary<MethodSignature, Type> ourDelegateTypes = new ConcurrentDictionary<MethodSignature, Type>();

        private static Type GetOrCreateDelegateType(MethodSignature signature, System.Reflection.MethodInfo managedMethod)
        {
            return ourDelegateTypes.GetOrAdd(signature, (_, managedMethodInner) => CreateDelegateType(managedMethodInner), managedMethod);
        }

        private static AssemblyBuilder ourAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Il2CppTrampolineDelegates"), AssemblyBuilderAccess.Run);
        private static ModuleBuilder ourModuleBuilder = ourAssemblyBuilder.DefineDynamicModule("Il2CppTrampolineDelegates");

        private static Type CreateDelegateType(System.Reflection.MethodInfo managedMethodInner)
        {
            var newType = ourModuleBuilder.DefineType("Il2CppToManagedDelegate_" + ExtractSignature(managedMethodInner), TypeAttributes.Sealed | TypeAttributes.Public, typeof(MulticastDelegate));
            newType.SetCustomAttribute(new CustomAttributeBuilder(typeof(UnmanagedFunctionPointerAttribute).GetConstructor(new []{typeof(CallingConvention)})!, new object[]{CallingConvention.Cdecl}));

            var ctor = newType.DefineConstructor(MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Public, CallingConventions.HasThis, new []{typeof(object), typeof(IntPtr)});
            ctor.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);

            var managedParameters = managedMethodInner.GetParameters();
            var parameterTypes = new Type[managedParameters.Length + 1];
            
            parameterTypes[managedParameters.Length] = typeof(MethodInfo*);
            for (var i = 0; i < managedParameters.Length; i++)
            {
                parameterTypes[i] = managedParameters[i].ParameterType.IsValueType
                    ? managedParameters[i].ParameterType
                    : typeof(IntPtr);
            }

            newType.DefineMethod("Invoke",
                MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Public,
                CallingConventions.HasThis,
                managedMethodInner.ReturnType.IsValueType ? managedMethodInner.ReturnType : typeof(IntPtr),
                parameterTypes).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);

            newType.DefineMethod("BeginInvoke",
                MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Public,
                CallingConventions.HasThis, typeof(IAsyncResult),
                parameterTypes.Concat(new[] {typeof(AsyncCallback), typeof(object)}).ToArray()).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);

            newType.DefineMethod("EndInvoke",
                MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Public,
                CallingConventions.HasThis,
                managedMethodInner.ReturnType.IsValueType ? managedMethodInner.ReturnType : typeof(IntPtr),
                new[] {typeof(IAsyncResult)}).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            
            return newType.CreateType();
        }

        private static string ExtractSignature(System.Reflection.MethodInfo methodInfo)
        {
            var builder = new StringBuilder();
            builder.Append(methodInfo.ReturnType.FullName);
            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                builder.Append('_');
                builder.Append(parameterInfo.ParameterType.FullName);
            }

            return builder.ToString();
        }


        private static readonly ConcurrentDictionary<System.Reflection.MethodInfo, Delegate> ourNativeToManagedTrampolines = new ConcurrentDictionary<System.Reflection.MethodInfo, Delegate>();

        private static Delegate GetOrCreateNativeToManagedTrampoline(MethodSignature signature, Il2CppSystem.Reflection.MethodInfo nativeMethod, System.Reflection.MethodInfo managedMethod)
        {
            return ourNativeToManagedTrampolines.GetOrAdd(managedMethod,
                (_, tuple) => GenerateNativeToManagedTrampoline(tuple.nativeMethod, tuple.managedMethod, tuple.signature), (nativeMethod, managedMethod, signature));
        }

        private static Delegate GenerateNativeToManagedTrampoline(Il2CppSystem.Reflection.MethodInfo nativeMethod,
            System.Reflection.MethodInfo managedMethod, MethodSignature signature)
        {
            var returnType = nativeMethod.ReturnType.IsValueType
                ? managedMethod.ReturnType
                : typeof(IntPtr);

            var managedParameters = managedMethod.GetParameters();
            var nativeParameters = nativeMethod.GetParameters();
            var parameterTypes = new Type[managedParameters.Length + 1];
            parameterTypes[managedParameters.Length] = typeof(MethodInfo*);
            for (var i = 0; i < managedParameters.Length; i++)
            {
                parameterTypes[i] = nativeParameters[i].ParameterType.IsValueType
                    ? managedParameters[i].ParameterType
                    : typeof(IntPtr);
            }
            
            var trampoline = new DynamicMethod("(il2cpp delegate trampoline) " + ExtractSignature(managedMethod), MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, typeof(DelegateSupport), true);
            var bodyBuilder = trampoline.GetILGenerator();

            var tryLabel = bodyBuilder.BeginExceptionBlock();

            bodyBuilder.Emit(OpCodes.Ldarg, managedParameters.Length);
            bodyBuilder.Emit(OpCodes.Ldc_I4, (int) Marshal.OffsetOf<MethodInfo>(nameof(MethodInfo.invoker_method)));
            bodyBuilder.Emit(OpCodes.Add);
            bodyBuilder.Emit(OpCodes.Ldind_I);
            bodyBuilder.Emit(OpCodes.Call, typeof(DelegateSupport).GetMethod(nameof(GetDelegate))!);

            for (var i = 0; i < managedParameters.Length; i++)
            {
                var parameterType = managedParameters[i].ParameterType;
                
                bodyBuilder.Emit(OpCodes.Ldarg, i);
                if (parameterType == typeof(string))
                {
                    bodyBuilder.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.Il2CppStringToManaged))!);
                }
                else if (!parameterType.IsValueType)
                {
                    var labelNull = bodyBuilder.DefineLabel();
                    var labelDone = bodyBuilder.DefineLabel();
                    bodyBuilder.Emit(OpCodes.Brfalse, labelNull);
                    bodyBuilder.Emit(OpCodes.Ldarg, i);
                    bodyBuilder.Emit(OpCodes.Newobj, parameterType.GetConstructor(new[] {typeof(IntPtr)})!);
                    bodyBuilder.Emit(OpCodes.Br, labelDone);
                    bodyBuilder.MarkLabel(labelNull);
                    bodyBuilder.Emit(OpCodes.Ldnull);
                    bodyBuilder.MarkLabel(labelDone);
                }
            }
            
            bodyBuilder.Emit(OpCodes.Call, managedMethod);
            
            if (returnType == typeof(string))
                bodyBuilder.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.ManagedStringToIl2Cpp))!);
            else if (!returnType.IsValueType)
            {
                var labelNull = bodyBuilder.DefineLabel();
                var labelDone = bodyBuilder.DefineLabel();
                bodyBuilder.Emit(OpCodes.Dup);
                bodyBuilder.Emit(OpCodes.Brfalse, labelNull);
                bodyBuilder.Emit(OpCodes.Call, typeof(Il2CppObjectBase).GetProperty(nameof(Il2CppObjectBase.Pointer))!.GetMethod);
                bodyBuilder.Emit(OpCodes.Br, labelDone);
                bodyBuilder.MarkLabel(labelNull);
                bodyBuilder.Emit(OpCodes.Pop);
                bodyBuilder.Emit(OpCodes.Ldc_I4_0);
                bodyBuilder.Emit(OpCodes.Conv_I);
                bodyBuilder.MarkLabel(labelDone);
            }

            var exceptionLocal = bodyBuilder.DeclareLocal(typeof(Exception));
            bodyBuilder.BeginCatchBlock(typeof(Exception));
            bodyBuilder.Emit(OpCodes.Stloc, exceptionLocal);
            bodyBuilder.Emit(OpCodes.Ldstr, "Exception in IL2CPP-to-Managed trampoline, not passing it to il2cpp: ");
            bodyBuilder.Emit(OpCodes.Ldloc, exceptionLocal);
            bodyBuilder.Emit(OpCodes.Callvirt, typeof(object).GetMethod(nameof(ToString))!);
            bodyBuilder.Emit(OpCodes.Call, typeof(string).GetMethod(nameof(string.Concat), new []{typeof(string), typeof(string)})!);
            bodyBuilder.Emit(OpCodes.Call, typeof(LogSupport).GetMethod(nameof(LogSupport.Error))!);
            
            bodyBuilder.EndExceptionBlock();
            
            bodyBuilder.Emit(OpCodes.Ret);

            return trampoline.CreateDelegate(GetOrCreateDelegateType(signature, managedMethod));
        }

        public static TIl2Cpp ConvertDelegate<TIl2Cpp>(Delegate @delegate) where TIl2Cpp : Il2CppObjectBase
        {
            if (@delegate == null)
                return null;
            
            if(!typeof(Il2CppSystem.Delegate).IsAssignableFrom(typeof(TIl2Cpp)))
                throw new ArgumentException($"{typeof(TIl2Cpp)} is not a delegate");
            
            var managedInvokeMethod = @delegate.GetType().GetMethod("Invoke")!;
            var parameterInfos = managedInvokeMethod.GetParameters();
            foreach (var parameterInfo in parameterInfos)
            {
                var parameterType = parameterInfo.ParameterType;
                if (parameterType.IsGenericParameter)
                    throw new ArgumentException($"Delegate has unsubstituted generic parameter ({parameterType}) which is not supported");
                
                if (parameterType.BaseType == typeof(Il2CppSystem.ValueType))
                    throw new ArgumentException($"Delegate has parameter of type {parameterType} (non-blittable struct) which is not supported");
            }

            var classTypePtr = Il2CppClassPointerStore<TIl2Cpp>.NativeClassPtr;
            if (classTypePtr == IntPtr.Zero)
                throw new ArgumentException($"Type {typeof(TIl2Cpp)} has uninitialized class pointer");

            var il2CppDelegateType = Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(classTypePtr));
            var nativeDelegateInvokeMethod = il2CppDelegateType.GetMethod("Invoke");

            var nativeParameters = nativeDelegateInvokeMethod.GetParameters();
            if (nativeParameters.Count != parameterInfos.Length)
                throw new ArgumentException($"Managed delegate has {parameterInfos.Length} parameters, native has {nativeParameters.Count}, these should match");

            for (var i = 0; i < nativeParameters.Count; i++)
            {
                var nativeType = nativeParameters[i].ParameterType;
                var managedType = parameterInfos[i].ParameterType;

                if (nativeType.IsPrimitive || managedType.IsPrimitive)
                {
                    if (nativeType.FullName != managedType.FullName)
                        throw new ArgumentException($"Parameter type mismatch at parameter {i}: {nativeType.FullName} != {managedType.FullName}");
                    
                    continue;
                }

                var classPointerFromManagedType = (IntPtr) typeof(Il2CppClassPointerStore<>).MakeGenericType(managedType)
                    .GetField(nameof(Il2CppClassPointerStore<int>.NativeClassPtr)).GetValue(null);

                var classPointerFromNativeType = IL2CPP.il2cpp_class_from_type(nativeType._impl.value);
                
                if (classPointerFromManagedType != classPointerFromNativeType)
                    throw new ArgumentException($"Parameter type at {i} has mismatched native type pointers; types: {nativeType.FullName} != {managedType.FullName}");
                
                if (nativeType.IsByRef || managedType.IsByRef)
                    throw new ArgumentException($"Parameter at {i} is passed by reference, this is not supported");
            }

            var signature = new MethodSignature(nativeDelegateInvokeMethod);
            var managedTrampoline =
                GetOrCreateNativeToManagedTrampoline(signature, nativeDelegateInvokeMethod, managedInvokeMethod);

            var nativeDelegatePtr = IL2CPP.il2cpp_object_new(classTypePtr);
            var converted = new Il2CppSystem.Delegate(nativeDelegatePtr);

            converted.method_ptr = Marshal.GetFunctionPointerForDelegate(managedTrampoline);
            converted.method_info = nativeDelegateInvokeMethod; // todo: is this truly a good hack?
            ourDelegates[ourCounter] = @delegate;
            var methodInfoSize = Marshal.SizeOf<MethodInfo>();
            var methodInfoPointer = Marshal.AllocHGlobal(methodInfoSize);
            var methodInfo = (MethodInfo*) methodInfoPointer;
            *methodInfo = default; // zero out everything
            converted.method = methodInfoPointer;

            methodInfo->flags = 0x10;
            methodInfo->methodPointer = converted.method_ptr;
            methodInfo->invoker_method = ourCounter; // todo: use target instead of invoker_method, use that for gc too
            methodInfo->parameters_count = (byte) parameterInfos.Length;
            methodInfo->slot = 65535;
            methodInfo->extra_flags = 0x8;
            
            /*
            MethodInfo* newMethod = (MethodInfo*)IL2CPP_CALLOC(1, sizeof(MethodInfo));
            newMethod->methodPointer = nativeFunctionPointer;
            newMethod->invoker_method = NULL;
            newMethod->parameters_count = invoke->parameters_count;
            newMethod->slot = kInvalidIl2CppMethodSlot;
            newMethod->is_marshaled_from_native = true;
             */
            
            ourCounter = IntPtr.Add(ourCounter, 1);
            return converted.Cast<TIl2Cpp>();
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MethodInfo
        {
            public IntPtr methodPointer;
            public IntPtr invoker_method;
            public IntPtr name; // const char*
            public IntPtr klass; // il2cppclass
            public IntPtr return_type; // il2cpptype
            public IntPtr parameters; // parameterinfo*

            public IntPtr someRtData;
            /*union
            {
                const Il2CppRGCTXData* rgctx_data; /* is_inflated is true and is_generic is false, i.e. a generic instance method #1#
                const Il2CppMethodDefinition* methodDefinition;
            };*/

            public IntPtr someGenericData;
            /*/* note, when is_generic == true and is_inflated == true the method represents an uninflated generic method on an inflated type. #1#
            union
            {
                const Il2CppGenericMethod* genericMethod; /* is_inflated is true #1#
                const Il2CppGenericContainer* genericContainer; /* is_inflated is false and is_generic is true #1#
            };*/

            public uint token;
            public ushort flags;
            public ushort iflags;
            public ushort slot;
            public byte parameters_count;
            public byte extra_flags;
            /*uint8_t is_generic : 1; /* true if method is a generic method definition #1#
            uint8_t is_inflated : 1; /* true if declaring_type is a generic instance or if method is a generic instance#1#
            uint8_t wrapper_type : 1; /* always zero (MONO_WRAPPER_NONE) needed for the debugger #1#
            uint8_t is_marshaled_from_native : 1*/
        }

        private class MethodSignature : IEquatable<MethodSignature>
        {
            private readonly IntPtr myReturnType;
            private readonly IntPtr[] myParameterTypes;

            public MethodSignature(Il2CppSystem.Reflection.MethodInfo methodInfo)
            {
                myReturnType = methodInfo.ReturnType.IsValueType ? methodInfo.ReturnType._impl.value : IntPtr.Zero;
                myParameterTypes = methodInfo.GetParameters().Select(it => it.ParameterType.IsValueType ? it.ParameterType._impl.value : IntPtr.Zero).ToArray();
            }

            public bool Equals(MethodSignature other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                if (!myReturnType.Equals(other.myReturnType)) return false;
                if (myParameterTypes.Length != other.myParameterTypes.Length) return false;
                for (var i = 0; i < myParameterTypes.Length; i++)
                    if (myParameterTypes[i] != other.myParameterTypes[i])
                        return false;

                return true;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((MethodSignature) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = myReturnType.GetHashCode();
                    foreach (var parameterType in myParameterTypes)
                        hashCode = hashCode * 397 + parameterType.GetHashCode();

                    return hashCode;
                }
            }

            public static bool operator ==(MethodSignature left, MethodSignature right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(MethodSignature left, MethodSignature right)
            {
                return !Equals(left, right);
            }
        }
    }
}