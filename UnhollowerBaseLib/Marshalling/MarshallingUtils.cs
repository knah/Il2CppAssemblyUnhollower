using System;
using System.Runtime.InteropServices;
using UnhollowerBaseLib.Maps;

namespace UnhollowerBaseLib
{
    public static class MarshallingUtils
    {
        public static readonly TypeTokensMap TokensMap = new(GeneratedDatabasesUtil.GetDatabasePath(TypeTokensMap.FileName));

        /// <summary>
        /// Returns a managed wrapper for a given pointer
        /// Only object pointers (including boxed value types) are supported
        /// </summary>
        public static Il2CppObjectBase MarshalObjectFromPointer(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero) return null;

            var actualType = TokensMap.LookupByObject(pointer);
            if (actualType == null)
            {
                var nativeClassName = Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(IL2CPP.il2cpp_object_get_class(pointer)));
                LogSupport.Warning($"Native object of native type {nativeClassName} doesn't have corresponding managed type; will use {nameof(Il2CppObjectBase)}; it implies a bug in unhollower!");
                return new Il2CppObjectBase(pointer);
            }

            return GenericMarshallingUtils.CreateNewInstance<Il2CppObjectBase>(pointer, actualType);
        }

        #region Static Field Marshallers

        public static unsafe T GetStaticBlittableField<T>(IntPtr fieldInfo) where T : unmanaged
        {
            T store = default;
            IL2CPP.il2cpp_field_static_get_value(fieldInfo, &store);
            return store;
        }

        public static unsafe T GetStaticNonBlittableField<T>(IntPtr fieldInfo) where T : IIl2CppNonBlittableValueType
        {
            uint _ = 0;
            var fieldType = IL2CPP.il2cpp_field_get_type(fieldInfo);
            var store = stackalloc byte[IL2CPP.il2cpp_class_value_size(fieldType, ref _)];
            IL2CPP.il2cpp_field_static_get_value(fieldInfo, store);
            return (T)(object)MarshalObjectFromPointer(IL2CPP.il2cpp_value_box(fieldType, (IntPtr)store));
        }

        public static unsafe T GetStaticReferenceField<T>(IntPtr fieldInfo) where T : class => (T)(object)MarshalObjectFromPointer(GetStaticBlittableField<IntPtr>(fieldInfo));


        public static unsafe void SetStaticBlittableField<T>(IntPtr fieldInfo, T value) where T : unmanaged => IL2CPP.il2cpp_field_static_set_value(fieldInfo, &value);
        public static unsafe void SetStaticNonBlittableField(IntPtr fieldInfo, IIl2CppNonBlittableValueType value) => IL2CPP.il2cpp_field_static_set_value(fieldInfo, (void*)value.ObjectBytesPointer);
        public static unsafe void SetStaticReferenceField(IntPtr fieldInfo, Il2CppObjectBase value) => SetStaticBlittableField(fieldInfo, value?.PointerNullable ?? IntPtr.Zero);
        public static unsafe void SetStaticInterfaceField<T>(IntPtr fieldInfo, T value)
        {
            if (value == null)
            {
                SetStaticBlittableField(fieldInfo, IntPtr.Zero);
                return;
            }

            // todo: handle non-boxed value types
            if (value is Il2CppObjectBase objectBase)
                SetStaticBlittableField(fieldInfo, objectBase.PointerNullable);
            else
                throw new NotImplementedException("Can't automatically convert non-injected types");
        }

        #endregion


        #region Non-static field and storage marshallers

        public static unsafe T ReadBlittableField<T>(IntPtr fieldPointer) where T : unmanaged => *(T*)fieldPointer;
        public static unsafe T ReadNonBlittableField<T>(IntPtr fieldPointer) where T : IIl2CppNonBlittableValueType => (T)(object)MarshalObjectFromPointer(IL2CPP.il2cpp_value_box(Il2CppClassPointerStore<T>.NativeClassPtr, fieldPointer));
        public static unsafe T ReadReferenceField<T>(IntPtr fieldPointer) => (T)(object)MarshalObjectFromPointer(ReadBlittableField<IntPtr>(fieldPointer));
        // nullables are handled by Il2CppNullable.ReadFromStorage

        public static unsafe void WriteBlittableField<T>(IntPtr fieldPointer, T value) where T : unmanaged => *(T*)fieldPointer = value;
        public static unsafe void WriteNonBlittableField(IntPtr fieldPointer, IIl2CppNonBlittableValueType value) => value.ObjectBytes.CopyTo(new Span<byte>((void*)fieldPointer, value.ObjectBytes.Length));
        public static unsafe void WriteReferenceField(IntPtr fieldPointer, Il2CppObjectBase value)
        {
            // todo: check that first argument is unused on all il2cpp versions
            // todo: support unity versions that don't have this export
            IL2CPP.il2cpp_gc_wbarrier_set_field(IntPtr.Zero, fieldPointer, value?.PointerNullable ?? IntPtr.Zero);
        }
        public static unsafe void WriteInterfaceField<T>(IntPtr fieldPointer, T value)
        {
            if (value == null)
            {
                WriteReferenceField(fieldPointer, null);
                return;
            }

            // todo: handle non-boxed value types
            if (value is Il2CppObjectBase objectBase)
                WriteReferenceField(fieldPointer, objectBase);
            else
                throw new NotImplementedException("Can't automatically convert non-injected types");
        }
        public static unsafe void WriteNullableField<T>(IntPtr fieldPointer, T value) where T : IIl2CppNullable => value.WriteToStorage(fieldPointer);

        #endregion


        #region Method Return Marshallers

        public static unsafe T MarshalBlittableMethodReturn<T>(IntPtr returnValue) where T : unmanaged => ReadBlittableField<T>(IL2CPP.il2cpp_object_unbox(returnValue));
        public static unsafe T MarshalNonBlittableMethodReturn<T>(IntPtr returnValue) where T : IIl2CppNonBlittableValueType => (T)(object)MarshalObjectFromPointer(returnValue);
        public static unsafe T MarshalReferenceMethodReturn<T>(IntPtr returnValue) => (T)(object)MarshalObjectFromPointer(returnValue);
        // nullables are handled in Il2CppNullable class

        #endregion

        #region Method Parameter Marshallers

        public static unsafe IntPtr MarshalBlittableMethodParameter<T>(ref T value) where T : unmanaged
        {
            return GenericMarshallingUtils.UnsafeGetPointer(ref value);
        }

        public static unsafe IntPtr MarshalNonBlittableMethodParameter<T>(ref T value) where T : IIl2CppNonBlittableValueType => value.ObjectBytesPointer;
        public static unsafe IntPtr MarshalReferenceMethodParameter<T>(ref T value) where T : Il2CppObjectBase => value?.PointerNullable ?? IntPtr.Zero;
        public static unsafe IntPtr MarshalNullableMethodParameter<T>(ref T value) where T : IIl2CppNullable => value.WriteForMethodCall();
        public static unsafe IntPtr MarshalInterfaceMethodParameter<T>(ref T value)
        {
            if (value == null)
                return IntPtr.Zero;

            if (value is Il2CppObjectBase objectBase)
                return objectBase.PointerNullable;

            // todo: handle non-boxed value types

            throw new NotImplementedException("Can't automatically convert non-injected types");
        }

        #endregion

        #region Method ByRef Parameter Marshallers

        public static unsafe IntPtr MarshalBlittableMethodParameterByRef<T>(ref T value, ref IntPtr scratchArea) where T : unmanaged
        {
            return GenericMarshallingUtils.UnsafeGetPointer(ref value);
        }

        public static unsafe IntPtr MarshalNullableMethodParameterByRef<T>(ref T value, ref IntPtr scratchArea) where T : IIl2CppNullable
        {
            return scratchArea = value.WriteForMethodCall();
        }

        public static unsafe IntPtr MarshalNonBlittableMethodParameterByRef<T>(ref T value, ref IntPtr scratchArea) where T : IIl2CppNonBlittableValueType => value.ObjectBytesPointer;
        public static unsafe IntPtr MarshalReferenceMethodParameterByRef<T>(ref T value, ref IntPtr scratchArea) where T : Il2CppObjectBase
        {
            scratchArea = value?.PointerNullable ?? IntPtr.Zero;
            return MarshalBlittableMethodParameter(ref scratchArea);
        }

        public static unsafe IntPtr MarshalInterfaceMethodParameterByRef<T>(ref T value, ref IntPtr scratchArea)
        {
            Il2CppObjectBase objectBase = null;

            if (value != null)
            {
                // todo: handle non-boxed value types
                if (value is Il2CppObjectBase @base)
                    objectBase = @base;
                else
                    throw new NotImplementedException("Can't automatically convert non-injected types");
            }

            return MarshalReferenceMethodParameterByRef(ref objectBase, ref scratchArea);
        }

        #endregion

        #region Method ByRef Parameter Restorers

        public static unsafe void MarshalBlittableMethodParameterByRefRestore<T>(ref T value, ref IntPtr scratchArea) where T : unmanaged
        {
            // no-op
        }

        public static unsafe void MarshalNonBlittableMethodParameterByRefRestore<T>(ref T value, ref IntPtr scratchArea)
            where T : IIl2CppNonBlittableValueType
        {
            // no-op
        }

        public static unsafe void MarshalNullableMethodParameterByRefRestore<T>(ref T value, ref IntPtr scratchArea)
            where T : IIl2CppNullable
        {
            value.ReplaceContentsIfNecessary(scratchArea);
        }

        public static unsafe void MarshalReferenceMethodParameterByRefRestore<T>(ref T value, ref IntPtr scratchArea) where T : Il2CppObjectBase
        {
            value = (T)MarshalObjectFromPointer(scratchArea);
        }

        public static unsafe void MarshalInterfaceMethodParameterByRefRestore<T>(ref T value, ref IntPtr scratchArea)
        {
            // todo: handle non-boxed value types better?
            value = (T)(object)MarshalObjectFromPointer(scratchArea);
        }

        #endregion

        public static unsafe ref TTo ReinterpretCast<TFrom, TTo>(ref TFrom from) where TTo : unmanaged where TFrom : unmanaged
        {
            fixed (TFrom* ptr = &from)
                return ref *(TTo*)(ptr);
        }
    }
}
