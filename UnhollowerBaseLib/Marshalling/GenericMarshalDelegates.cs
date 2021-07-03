using System;
using System.Reflection;

namespace UnhollowerBaseLib
{
    public static class GenericMarshallingMethods
    {
        public static MethodInfo StaticFieldGetterBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.GetStaticBlittableField));
        public static MethodInfo StaticFieldGetterNonBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.GetStaticNonBlittableField));
        public static MethodInfo StaticFieldGetterReference = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.GetStaticReferenceField));
        
        public static MethodInfo StaticFieldSetterBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.SetStaticBlittableField));
        public static MethodInfo StaticFieldSetterNonBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.SetStaticNonBlittableField));
        public static MethodInfo StaticFieldSetterReference = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.SetStaticReferenceField));
        public static MethodInfo StaticFieldSetterInterface = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.SetStaticInterfaceField));
        
        
        public static MethodInfo FieldOrStoreSetterBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.WriteBlittableField));
        public static MethodInfo FieldOrStoreSetterNonBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.WriteNonBlittableField));
        public static MethodInfo FieldOrStoreSetterReference = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.WriteReferenceField));
        public static MethodInfo FieldOrStoreSetterInterface = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.WriteInterfaceField));
        public static MethodInfo FieldOrStoreSetterNullable = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.WriteNullableField));
        
        public static MethodInfo FieldOrStoreGetterBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.ReadBlittableField));
        public static MethodInfo FieldOrStoreGetterNonBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.ReadNonBlittableField));
        public static MethodInfo FieldOrStoreGetterReference = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.ReadReferenceField));
        // nullables are handled by Il2CppNullable generic class
        
        public static MethodInfo MethodReturnBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalBlittableMethodReturn));
        public static MethodInfo MethodReturnNonBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalNonBlittableMethodReturn));
        public static MethodInfo MethodReturnReference = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalReferenceMethodReturn));
        // nullables are handled by Il2CppNullable generic class
        
        public static MethodInfo MethodParameterBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalBlittableMethodParameter));
        public static MethodInfo MethodParameterNonBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalNonBlittableMethodParameter));
        public static MethodInfo MethodParameterReference = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalReferenceMethodParameter));
        public static MethodInfo MethodParameterInterface = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalInterfaceMethodParameter));
        public static MethodInfo MethodParameterNullable = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalNullableMethodParameter));
        
        public static MethodInfo MethodParameterByRefBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalBlittableMethodParameterByRef));
        public static MethodInfo MethodParameterByRefNonBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalNonBlittableMethodParameterByRef));
        public static MethodInfo MethodParameterByRefReference = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalReferenceMethodParameterByRef));
        public static MethodInfo MethodParameterByRefInterface = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalInterfaceMethodParameterByRef));
        public static MethodInfo MethodParameterByRefNullable = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalNullableMethodParameterByRef));
        
        public static MethodInfo MethodParameterByRefRestoreBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalBlittableMethodParameterByRefRestore));
        public static MethodInfo MethodParameterByRefRestoreNonBlittalble = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalNonBlittableMethodParameterByRefRestore));
        public static MethodInfo MethodParameterByRefRestoreReference = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalReferenceMethodParameterByRefRestore));
        public static MethodInfo MethodParameterByRefRestoreInterface = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalInterfaceMethodParameterByRefRestore));
        public static MethodInfo MethodParameterByRefRestoreNullable = typeof(MarshallingUtils).GetMethod(nameof(MarshallingUtils.MarshalNullableMethodParameterByRefRestore));
    }
    
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