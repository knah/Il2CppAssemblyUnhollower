using System;
using System.Diagnostics;

namespace UnhollowerBaseLib
{
    public interface IIl2CppNullable // non-generic interface for type checks and writes
    {
        public void WriteToStorage(IntPtr pointer);
        public IntPtr WriteForMethodCall();
        public void ReplaceContentsIfNecessary(IntPtr newData);
    }
    
    public struct Il2CppNullable<T>: IIl2CppNullable
    {
        public T Value { get; private set; }
        public bool HasValue { get; private set; }

        public static unsafe Il2CppNullable<T> ReadFromStorage(IntPtr pointer)
        {
            var result = new Il2CppNullable<T>();
            if (pointer != IntPtr.Zero)
            {
                uint _ = 0;
                var valueSize = IL2CPP.il2cpp_class_value_size(Il2CppClassPointerStore<T>.NativeClassPtr, ref _);
                result.HasValue = ((byte*) pointer)[valueSize] != 0;
                if (result.HasValue) 
                    result.Value = GenericMarshallingUtils.ReadFieldGeneric<T>(pointer);
            }

            return result;
        }

        /// <summary>
        /// Methods return boxed objects, so a method returning a nullable will return either null for no-value, or a boxed inner value for has-value
        /// </summary>
        public static Il2CppNullable<T> ReadFromMethodReturn(IntPtr pointer)
        {
            var result = new Il2CppNullable<T>();
            if (pointer != IntPtr.Zero)
            {
                result.HasValue = true;
                result.Value = GenericMarshallingUtils.MarshalGenericMethodReturn<T>(pointer);
            }

            return result;
        }

        public IntPtr WriteForMethodCall()
        {
            uint _ = 0;
            var valueSize = IL2CPP.il2cpp_class_value_size(Il2CppClassPointerStore<T>.NativeClassPtr, ref _);

            var valueStore = MethodCallScratchSpaceAllocator.AllocateScratchSpace(valueSize + 1);
            
            WriteToStorage(valueStore);
            
            return valueStore;
        }

        public void ReplaceContentsIfNecessary(IntPtr newData)
        {
            if (typeof(T).IsValueType)
            {
                var updated = ReadFromStorage(newData);
                HasValue = updated.HasValue;
                Value = updated.Value;
            }
        }

        public unsafe void WriteToStorage(IntPtr pointer)
        {
            uint _ = 0;
            var valueSize = IL2CPP.il2cpp_class_value_size(Il2CppClassPointerStore<T>.NativeClassPtr, ref _);
            if (HasValue)
            {
                ((byte*) pointer)[valueSize] = 1;
                GenericMarshallingUtils.WriteFieldGeneric(pointer, Value);
            }
            else
            {
                ((byte*) pointer)[valueSize] = 0;
            }
        }
    }
}