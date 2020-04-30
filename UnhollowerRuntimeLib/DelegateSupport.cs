using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using UnhollowerBaseLib;
using Object = Il2CppSystem.Object;
using ValueType = Il2CppSystem.ValueType;

namespace UnhollowerRuntimeLib
{
    public static unsafe class DelegateSupport
    {
        private static readonly ConcurrentDictionary<MethodSignature, Type> ourDelegateTypes = new ConcurrentDictionary<MethodSignature, Type>();

        internal static Type GetOrCreateDelegateType(MethodSignature signature, MethodInfo managedMethod)
        {
            return ourDelegateTypes.GetOrAdd(signature, (signature, managedMethodInner) => CreateDelegateType(managedMethodInner, signature.HasThis, signature.ConstructedFromNative), managedMethod);
        }

        private static readonly AssemblyBuilder AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Il2CppTrampolineDelegates"), AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder ModuleBuilder = AssemblyBuilder.DefineDynamicModule("Il2CppTrampolineDelegates");

        private static Type CreateDelegateType(MethodInfo managedMethodInner, bool addIntPtrForThis, bool addNamingDisambig)
        {
            var newType = ModuleBuilder.DefineType("Il2CppToManagedDelegate_" + ExtractSignature(managedMethodInner) + (addIntPtrForThis ? "HasThis" : "") + (addNamingDisambig ? "FromNative" : ""), TypeAttributes.Sealed | TypeAttributes.Public, typeof(MulticastDelegate));
            newType.SetCustomAttribute(new CustomAttributeBuilder(typeof(UnmanagedFunctionPointerAttribute).GetConstructor(new []{typeof(CallingConvention)})!, new object[]{CallingConvention.Cdecl}));

            var ctor = newType.DefineConstructor(MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Public, CallingConventions.HasThis, new []{typeof(object), typeof(IntPtr)});
            ctor.SetImplementationFlags(MethodImplAttributes.CodeTypeMask);

            var parameterOffset = addIntPtrForThis ? 1 : 0;
            var managedParameters = managedMethodInner.GetParameters();
            var parameterTypes = new Type[managedParameters.Length + 1 + parameterOffset];

            if (addIntPtrForThis)
                parameterTypes[0] = typeof(IntPtr);
            
            parameterTypes[parameterTypes.Length - 1] = typeof(Il2CppMethodInfo*);
            for (var i = 0; i < managedParameters.Length; i++)
            {
                parameterTypes[i + parameterOffset] = managedParameters[i].ParameterType.IsValueType
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

        private static string ExtractSignature(MethodInfo methodInfo)
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


        private static readonly ConcurrentDictionary<MethodInfo, Delegate> NativeToManagedTrampolines = new ConcurrentDictionary<MethodInfo, Delegate>();

        private static Delegate GetOrCreateNativeToManagedTrampoline(MethodSignature signature, Il2CppSystem.Reflection.MethodInfo nativeMethod, MethodInfo managedMethod)
        {
            return NativeToManagedTrampolines.GetOrAdd(managedMethod,
                (_, tuple) => GenerateNativeToManagedTrampoline(tuple.nativeMethod, tuple.managedMethod, tuple.signature), (nativeMethod, managedMethod, signature));
        }

        private static Delegate GenerateNativeToManagedTrampoline(Il2CppSystem.Reflection.MethodInfo nativeMethod,
            MethodInfo managedMethod, MethodSignature signature)
        {
            var returnType = nativeMethod.ReturnType.IsValueType
                ? managedMethod.ReturnType
                : typeof(IntPtr);

            var managedParameters = managedMethod.GetParameters();
            var nativeParameters = nativeMethod.GetParameters();
            var parameterTypes = new Type[managedParameters.Length + 1 + 1]; // thisptr for target, methodInfo last arg
            parameterTypes[0] = typeof(IntPtr);
            parameterTypes[managedParameters.Length + 1] = typeof(Il2CppMethodInfo*);
            for (var i = 0; i < managedParameters.Length; i++)
            {
                parameterTypes[i + 1] = nativeParameters[i].ParameterType.IsValueType
                    ? managedParameters[i].ParameterType
                    : typeof(IntPtr);
            }
            
            var trampoline = new DynamicMethod("(il2cpp delegate trampoline) " + ExtractSignature(managedMethod), MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, typeof(DelegateSupport), true);
            var bodyBuilder = trampoline.GetILGenerator();

            var tryLabel = bodyBuilder.BeginExceptionBlock();

            bodyBuilder.Emit(OpCodes.Ldarg_0);
            bodyBuilder.Emit(OpCodes.Call, typeof(ClassInjector).GetMethod(nameof(ClassInjector.GetMonoObjectFromIl2CppPointer))!);
            bodyBuilder.Emit(OpCodes.Castclass, typeof(Il2CppToMonoDelegateReference));
            bodyBuilder.Emit(OpCodes.Ldfld, typeof(Il2CppToMonoDelegateReference).GetField(nameof(Il2CppToMonoDelegateReference.ReferencedDelegate)));

            for (var i = 0; i < managedParameters.Length; i++)
            {
                var parameterType = managedParameters[i].ParameterType;
                
                bodyBuilder.Emit(OpCodes.Ldarg, i + 1);
                if (parameterType == typeof(string))
                {
                    bodyBuilder.Emit(OpCodes.Call, typeof(IL2CPP).GetMethod(nameof(IL2CPP.Il2CppStringToManaged))!);
                }
                else if (!parameterType.IsValueType)
                {
                    var labelNull = bodyBuilder.DefineLabel();
                    var labelDone = bodyBuilder.DefineLabel();
                    bodyBuilder.Emit(OpCodes.Brfalse, labelNull);
                    bodyBuilder.Emit(OpCodes.Ldarg, i + 1);
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
                
                if (parameterType.BaseType == typeof(ValueType))
                    throw new ArgumentException($"Delegate has parameter of type {parameterType} (non-blittable struct) which is not supported");
            }

            var classTypePtr = Il2CppClassPointerStore<TIl2Cpp>.NativeClassPtr;
            if (classTypePtr == IntPtr.Zero)
                throw new ArgumentException($"Type {typeof(TIl2Cpp)} has uninitialized class pointer");
            
            if (Il2CppClassPointerStore<Il2CppToMonoDelegateReference>.NativeClassPtr == IntPtr.Zero)
                ClassInjector.RegisterTypeInIl2Cpp<Il2CppToMonoDelegateReference>();

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

            var signature = new MethodSignature(nativeDelegateInvokeMethod, true);
            var managedTrampoline =
                GetOrCreateNativeToManagedTrampoline(signature, nativeDelegateInvokeMethod, managedInvokeMethod);

            var nativeDelegatePtr = IL2CPP.il2cpp_object_new(classTypePtr);
            var converted = new Il2CppSystem.Delegate(nativeDelegatePtr);

            converted.method_ptr = Marshal.GetFunctionPointerForDelegate(managedTrampoline);
            converted.method_info = nativeDelegateInvokeMethod; // todo: is this truly a good hack?
            var methodInfoSize = Marshal.SizeOf<Il2CppMethodInfo>();
            var methodInfoPointer = Marshal.AllocHGlobal(methodInfoSize);
            var methodInfo = (Il2CppMethodInfo*) methodInfoPointer;
            *methodInfo = default; // zero out everything
            converted.method = methodInfoPointer;

            methodInfo->methodPointer = converted.method_ptr;
            methodInfo->invoker_method = IntPtr.Zero;
            methodInfo->parameters_count = (byte) parameterInfos.Length;
            methodInfo->slot = ushort.MaxValue;
            methodInfo->extra_flags = MethodInfoExtraFlags.is_marshalled_from_native;
            
            converted.m_target = new Il2CppToMonoDelegateReference(@delegate, methodInfoPointer);

            return converted.Cast<TIl2Cpp>();
        }

        internal class MethodSignature : IEquatable<MethodSignature>
        {
            public readonly bool HasThis;
            public readonly bool ConstructedFromNative;
            private readonly IntPtr myReturnType;
            private readonly IntPtr[] myParameterTypes;

            public MethodSignature(Il2CppSystem.Reflection.MethodInfo methodInfo, bool hasThis)
            {
                HasThis = hasThis;
                myReturnType = methodInfo.ReturnType.IsValueType ? methodInfo.ReturnType._impl.value : IntPtr.Zero;
                myParameterTypes = methodInfo.GetParameters().Select(it => it.ParameterType.IsValueType ? it.ParameterType._impl.value : IntPtr.Zero).ToArray();
                ConstructedFromNative = true;
            }
            
            public MethodSignature(MethodInfo methodInfo, bool hasThis)
            {
                HasThis = hasThis;
                myReturnType = methodInfo.ReturnType.IsValueType ? methodInfo.ReturnType.TypeHandle.Value : IntPtr.Zero;
                myParameterTypes = methodInfo.GetParameters().Select(it => it.ParameterType.IsValueType ? it.ParameterType.TypeHandle.Value : IntPtr.Zero).ToArray();
                ConstructedFromNative = false;
            }

            public bool Equals(MethodSignature other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                if (!myReturnType.Equals(other.myReturnType)) return false;
                if (HasThis != other.HasThis) return false;
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
                if (obj.GetType() != GetType()) return false;
                return Equals((MethodSignature) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = myReturnType.GetHashCode() ^ HasThis.GetHashCode();
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

        private class Il2CppToMonoDelegateReference : Object
        {
            public Delegate ReferencedDelegate;
            public IntPtr MethodInfo;
            
            public Il2CppToMonoDelegateReference(IntPtr obj0) : base(obj0)
            {
            }

            public Il2CppToMonoDelegateReference(Delegate referencedDelegate, IntPtr methodInfo) : base(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<Il2CppToMonoDelegateReference>.NativeClassPtr))
            {
                var ownGcHandle = GCHandle.Alloc(this, GCHandleType.Normal);
                ClassInjector.AssignGcHandle(Pointer, ownGcHandle);
                
                ReferencedDelegate = referencedDelegate;
                MethodInfo = methodInfo;
            }

            ~Il2CppToMonoDelegateReference()
            {
                LogSupport.Trace("Disposing delegate");
                Marshal.FreeHGlobal(MethodInfo);
                MethodInfo = IntPtr.Zero;
                ReferencedDelegate = null;
            }
        }
    }
}