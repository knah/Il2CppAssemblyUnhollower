using System.Reflection;

namespace UnhollowerBaseLib.Marshalling
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
}
