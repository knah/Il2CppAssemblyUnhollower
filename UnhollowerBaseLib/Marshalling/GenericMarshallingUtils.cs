using System;
using System.Collections.Concurrent;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib
{
    public static class GenericMarshallingUtils
    {
        private static readonly ConcurrentDictionary<Type, Func<IntPtr, IIl2CppObjectBase>> CachedConstructors = new();
        private static readonly ConcurrentDictionary<Type, Delegate> CachedUnsafePointerGetters = new();

        /// <summary>
        /// Returns a managed wrapper for a given pointer
        /// If T represents a non-blittable value type, `pointer` is assumed to be boxed
        /// </summary>
        public static T MarshalObjectFromPointerKnownTypeBound<T>(IntPtr pointer) where T : class, IIl2CppObjectBase
        {
            if (pointer == IntPtr.Zero) return null;

            var actualType = MarshallingUtils.TokensMap.LookupByObject(pointer);
            if (actualType == null)
            {
                var nativeClassName = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(IL2CPP.il2cpp_object_get_class(pointer)));
                LogSupport.Warning($"Native object of native type {nativeClassName} doesn't have corresponding managed type; will use {typeof(T)}; it implies a bug in unhollower!");
                actualType = typeof(T);
            }

            return CreateNewInstance<T>(pointer, actualType);
        }

        public static T CreateNewInstance<T>(IntPtr pointer, Type actualType) where T : class, IIl2CppObjectBase
        {
            var factory = (Func<IntPtr, T>)CachedConstructors.GetOrAdd(actualType, t =>
            {
                if (t.IsValueType) t = typeof(Il2CppBox<>).MakeGenericType(t);

                var ctor = t.GetConstructor(new[] { typeof(IntPtr) });
                if (ctor == null) throw new ArgumentException($"Type {t.FullName} doesn't have an IntPtr constructor");
                var dynamicMethod = new DynamicMethod($"(runtime bound IntPtr constructor for {t.AssemblyQualifiedName})", t,
                    new[] { typeof(IntPtr) });
                var body = dynamicMethod.GetILGenerator();
                body.Emit(OpCodes.Ldarg_0);
                body.Emit(OpCodes.Newobj, ctor);
                body.Emit(OpCodes.Ret);
                return (Func<IntPtr, T>)dynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(IntPtr), t));
            });

            return factory(pointer);
        }

        private delegate IntPtr GenericRefDelegate<T>(ref T value);

        internal static IntPtr UnsafeGetPointer<T>(ref T value)
        {
            return ((GenericRefDelegate<T>)CachedUnsafePointerGetters.GetOrAdd(typeof(T), t =>
            {
                var dynamicMethod = new DynamicMethod($"(runtime bound IntPtr getter for {t.AssemblyQualifiedName})", typeof(IntPtr),
                    new[] { typeof(T).MakeByRefType() });
                var body = dynamicMethod.GetILGenerator();
                body.Emit(OpCodes.Ldarg_0);
                body.Emit(OpCodes.Conv_I);
                body.Emit(OpCodes.Ret);
                return (GenericRefDelegate<T>)dynamicMethod.CreateDelegate(typeof(GenericRefDelegate<T>));
            }))(ref value);
        }

        /// <summary>
        /// Reads a value from given memory area.
        /// If `T` is a blittable value type or is derived from `Il2CppNonBlittableValueType`, memory area will be treated as value-type bytes.
        /// Otherwise the memory area will be treated as containing a (boxed) object pointer
        /// </summary>
        /// <param name="fieldStorePointer"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ReadFieldGeneric<T>(IntPtr fieldStorePointer)
        {
            return GenericMarshalDelegates<T>.FieldOrStoreGetter(fieldStorePointer);
        }

        /// <summary>
        /// Marshals a pointer returned from il2cpp_runtime_invoke, which is either an object pointer or a boxed pointer
        /// Except for when T is Nullable&lt;U&gt;, then it's a null pointer or a pointer to the boxed value
        /// </summary>
        /// <param name="returnedObject"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T MarshalGenericMethodReturn<T>(IntPtr returnedObject)
        {
            return GenericMarshalDelegates<T>.MethodReturn(returnedObject);
        }

        /// <summary>
        /// Writes a given managed object to specified memory area. Value types, including non-blittable ones, will be written as raw bytes.
        /// If `T` is not derived from `Il2CppNonBlittableValueType` but points to a boxed value type nonetheless
        ///     (either via Il2CppSystem.Object or by `value` having factual type of `Il2CppNonBlittableValueType`),
        ///     a boxed pointer will be written instead.
        /// </summary>
        public static void WriteFieldGeneric<T>(IntPtr fieldStorePointer, T value)
        {
            GenericMarshalDelegates<T>.FieldOrStoreSetter(fieldStorePointer, value);
        }

        public static void WriteStaticFieldGeneric<T>(IntPtr fieldInfo, T value)
        {
            GenericMarshalDelegates<T>.StaticFieldSetter(fieldInfo, value);
        }

        public static T ReadStaticFieldGeneric<T>(IntPtr fieldInfo)
        {
            return GenericMarshalDelegates<T>.StaticFieldGetter(fieldInfo);
        }

        public static IntPtr MarshalMethodParameter<T>(ref T value)
        {
            return GenericMarshalDelegates<T>.MethodParameter(ref value);
        }

        public static IntPtr MarshalMethodParameterByRef<T>(ref T value, ref IntPtr scratchArea)
        {
            return GenericMarshalDelegates<T>.MethodParameterByRef(ref value, ref scratchArea);
        }

        public static void MarshalMethodParameterByRefRestore<T>(ref T value, ref IntPtr scratchArea)
        {
            GenericMarshalDelegates<T>.MethodParameterByRefRestore(ref value, ref scratchArea);
        }

        /// <summary>
        /// Use case: calling methods with generic parameter types
        /// </summary>
        public static IntPtr MarshalManagedObjectToPointer<T>(ref T value)
        {
            var type = typeof(T);
            if (type == typeof(string))
            {
                return IL2CPP.ManagedStringToIl2Cpp(value as string);
            }

            if (typeof(IIl2CppNonBlittableValueType).IsAssignableFrom(type))
            {
                var nonBlittable = value as IIl2CppNonBlittableValueType;
                if (nonBlittable == null) throw new ArgumentNullException(nameof(value), "Null non-blittable value type passed to field setter");

                return nonBlittable.ObjectBytesPointer;
            }

            if (typeof(IIl2CppNullable).IsAssignableFrom(type))
                return ((IIl2CppNullable)value).WriteForMethodCall();

            if (typeof(Il2CppObjectBase).IsAssignableFrom(type))
                return (value as Il2CppObjectBase)?.PointerNullable ?? IntPtr.Zero;

            // remaining case: blittable value type
            return UnsafeGetPointer(ref value);
        }
    }
}
