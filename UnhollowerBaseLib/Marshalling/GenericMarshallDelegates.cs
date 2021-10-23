using System;
using System.Reflection;

namespace UnhollowerBaseLib.Marshalling
{
    public static class GenericMarshalDelegates<T>
    {
        public static Action<IntPtr, T> StaticFieldSetter;
        public static Func<IntPtr, T> StaticFieldGetter;

        public static Action<IntPtr, T> FieldOrStoreSetter;
        public static Func<IntPtr, T> FieldOrStoreGetter;

        public static Func<IntPtr, T> MethodReturn;

        public static MethodParameterDelegate MethodParameter;
        public static MethodParameterByRefDelegate MethodParameterByRef;
        public static MethodParameterByRefRestoreDelegate MethodParameterByRefRestore;

        public delegate IntPtr MethodParameterDelegate(ref T value);
        public delegate IntPtr MethodParameterByRefDelegate(ref T value, ref IntPtr scratchArea);
        public delegate void MethodParameterByRefRestoreDelegate(ref T value, ref IntPtr scratchArea);

        static GenericMarshalDelegates()
        {
            var type = typeof(T);
            try
            {
                if (typeof(IIl2CppNonBlittableValueType).IsAssignableFrom(type))
                {
                    StaticFieldGetter = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.StaticFieldGetterNonBlittalble.MakeGenericMethod(type));
                    StaticFieldSetter = CreateDelegate<Action<IntPtr, T>>(GenericMarshallingMethods.StaticFieldSetterNonBlittalble);

                    FieldOrStoreGetter = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.FieldOrStoreGetterNonBlittalble.MakeGenericMethod(type));
                    FieldOrStoreSetter = CreateDelegate<Action<IntPtr, T>>(GenericMarshallingMethods.FieldOrStoreSetterNonBlittalble);

                    MethodReturn = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.MethodReturnNonBlittalble.MakeGenericMethod(type));

                    MethodParameter = CreateDelegate<MethodParameterDelegate>(GenericMarshallingMethods.MethodParameterNonBlittalble.MakeGenericMethod(type));
                    MethodParameterByRef = CreateDelegate<MethodParameterByRefDelegate>(GenericMarshallingMethods.MethodParameterByRefNonBlittalble.MakeGenericMethod(type));
                    MethodParameterByRefRestore = CreateDelegate<MethodParameterByRefRestoreDelegate>(GenericMarshallingMethods.MethodParameterByRefRestoreNonBlittalble.MakeGenericMethod(type));

                    return;
                }

                if (type.IsInterface || type == typeof(object))
                {
                    StaticFieldGetter = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.StaticFieldGetterReference.MakeGenericMethod(type));
                    StaticFieldSetter = CreateDelegate<Action<IntPtr, T>>(GenericMarshallingMethods.StaticFieldSetterInterface);

                    FieldOrStoreGetter = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.FieldOrStoreGetterReference.MakeGenericMethod(type));
                    FieldOrStoreSetter = CreateDelegate<Action<IntPtr, T>>(GenericMarshallingMethods.FieldOrStoreSetterInterface);

                    MethodReturn = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.MethodReturnReference.MakeGenericMethod(type));

                    MethodParameter = CreateDelegate<MethodParameterDelegate>(GenericMarshallingMethods.MethodParameterInterface.MakeGenericMethod(type));
                    MethodParameterByRef = CreateDelegate<MethodParameterByRefDelegate>(GenericMarshallingMethods.MethodParameterByRefInterface.MakeGenericMethod(type));
                    MethodParameterByRefRestore = CreateDelegate<MethodParameterByRefRestoreDelegate>(GenericMarshallingMethods.MethodParameterByRefRestoreInterface.MakeGenericMethod(type));

                    return;
                }

                if (typeof(IIl2CppNullable).IsAssignableFrom(type))
                {
                    StaticFieldGetter = _ => throw new NotImplementedException("Can't get nullable static fields");
                    StaticFieldSetter = (_, _) => throw new NotImplementedException("Can't set nullable static fields");

                    FieldOrStoreGetter = CreateDelegate<Func<IntPtr, T>>(type.GetMethod(nameof(Il2CppNullable<int>.ReadFromStorage)));
                    FieldOrStoreSetter = CreateDelegate<Action<IntPtr, T>>(GenericMarshallingMethods.FieldOrStoreSetterNullable.MakeGenericMethod(type));

                    MethodReturn = CreateDelegate<Func<IntPtr, T>>(type.GetMethod(nameof(Il2CppNullable<int>.ReadFromMethodReturn)));

                    MethodParameter = CreateDelegate<MethodParameterDelegate>(GenericMarshallingMethods.MethodParameterNullable.MakeGenericMethod(type));
                    MethodParameterByRef = CreateDelegate<MethodParameterByRefDelegate>(GenericMarshallingMethods.MethodParameterByRefNullable.MakeGenericMethod(type));
                    MethodParameterByRefRestore = CreateDelegate<MethodParameterByRefRestoreDelegate>(GenericMarshallingMethods.MethodParameterByRefRestoreNullable.MakeGenericMethod(type));

                    return;
                }

                if (typeof(Il2CppObjectBase).IsAssignableFrom(type))
                {
                    StaticFieldGetter = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.StaticFieldGetterReference.MakeGenericMethod(type));
                    StaticFieldSetter = CreateDelegate<Action<IntPtr, T>>(GenericMarshallingMethods.StaticFieldSetterReference);

                    FieldOrStoreGetter = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.FieldOrStoreGetterReference.MakeGenericMethod(type));
                    FieldOrStoreSetter = CreateDelegate<Action<IntPtr, T>>(GenericMarshallingMethods.FieldOrStoreSetterReference);

                    MethodReturn = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.MethodReturnReference.MakeGenericMethod(type));

                    MethodParameter = CreateDelegate<MethodParameterDelegate>(GenericMarshallingMethods.MethodParameterReference.MakeGenericMethod(type));
                    MethodParameterByRef = CreateDelegate<MethodParameterByRefDelegate>(GenericMarshallingMethods.MethodParameterByRefReference.MakeGenericMethod(type));
                    MethodParameterByRefRestore = CreateDelegate<MethodParameterByRefRestoreDelegate>(GenericMarshallingMethods.MethodParameterByRefRestoreReference.MakeGenericMethod(type));

                    return;
                }

                StaticFieldGetter = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.StaticFieldGetterBlittalble.MakeGenericMethod(type));
                StaticFieldSetter = CreateDelegate<Action<IntPtr, T>>(GenericMarshallingMethods.StaticFieldSetterBlittalble.MakeGenericMethod(type));

                FieldOrStoreGetter = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.FieldOrStoreGetterBlittalble.MakeGenericMethod(type));
                FieldOrStoreSetter = CreateDelegate<Action<IntPtr, T>>(GenericMarshallingMethods.FieldOrStoreSetterBlittalble.MakeGenericMethod(type));

                MethodReturn = CreateDelegate<Func<IntPtr, T>>(GenericMarshallingMethods.MethodReturnBlittalble.MakeGenericMethod(type));

                MethodParameter = CreateDelegate<MethodParameterDelegate>(GenericMarshallingMethods.MethodParameterBlittalble.MakeGenericMethod(type));
                MethodParameterByRef = CreateDelegate<MethodParameterByRefDelegate>(GenericMarshallingMethods.MethodParameterByRefBlittalble.MakeGenericMethod(type));
                MethodParameterByRefRestore = CreateDelegate<MethodParameterByRefRestoreDelegate>(GenericMarshallingMethods.MethodParameterByRefRestoreBlittalble.MakeGenericMethod(type));
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Exception while producing marshalling delegates for type {type}: {ex}", ex);
            }
        }

        private static TDelegate CreateDelegate<TDelegate>(MethodInfo method) where TDelegate : Delegate => (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), method);
    }
}
