using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib
{
    public class Il2CppReferenceArray<T> : Il2CppArrayBase<T> where T: Il2CppObjectBase
    {
        private static ConstructorInfo ourCachedInstanceCtor;
        private static int ourElementTypeSize;
        private static bool ourElementIsValueType;
        
        public Il2CppReferenceArray(IntPtr nativeObject) : base(nativeObject)
        {
        }

        public Il2CppReferenceArray(long size) : base(AllocateArray(size))
        {
        }

        public Il2CppReferenceArray(T[] arr) : base(AllocateArray(arr.Length))
        {
            for (var i = 0; i < arr.Length; i++) 
                this[i] = arr[i];
        }

        static Il2CppReferenceArray()
        {
            ourElementTypeSize = IntPtr.Size;
            var nativeClassPtr = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nativeClassPtr == IntPtr.Zero) return;
            uint align = 0;
            if (IL2CPP.il2cpp_class_is_valuetype(nativeClassPtr))
            {
                ourElementIsValueType = true;
                ourElementTypeSize = IL2CPP.il2cpp_class_value_size(nativeClassPtr, ref align);
            }
        }
        
        public static implicit operator Il2CppReferenceArray<T>(T[] arr)
        {
            if (arr == null) return null;
            
            return new Il2CppReferenceArray<T>(arr);
        }

        public override T this[int index]
        {
            get
            {
                if(index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Array index may not be negative or above length of the array");
                var arrayStartPointer = IntPtr.Add(Pointer, 4 * IntPtr.Size);
                var elementPointer = IntPtr.Add(arrayStartPointer, index * ourElementTypeSize);
                return WrapElement(elementPointer);
            }
            set
            {
                if(index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Array index may not be negative or above length of the array");
                var arrayStartPointer = IntPtr.Add(Pointer, 4 * IntPtr.Size);
                var elementPointer = IntPtr.Add(arrayStartPointer, index * ourElementTypeSize);
                StoreValue(elementPointer, value?.Pointer ?? IntPtr.Zero);
            }
        }

        private static unsafe void StoreValue(IntPtr targetPointer, IntPtr valuePointer)
        {
            if (ourElementIsValueType)
            {
                if(valuePointer == IntPtr.Zero)
                    throw new NullReferenceException();
                
                var valueRawPointer = (byte*) IL2CPP.il2cpp_object_unbox(valuePointer);
                var targetRawPointer = (byte*) targetPointer;
                for (var i = 0; i < ourElementTypeSize; i++) 
                    targetRawPointer[i] = valueRawPointer[i];
            }
            else
            {
                *(IntPtr*) targetPointer = valuePointer;
            }
        }

        private static unsafe T WrapElement(IntPtr memberPointer)
        {
            if (ourCachedInstanceCtor == null)
            {
                ourCachedInstanceCtor = typeof(T).GetConstructor(new[] {typeof(IntPtr)});
            }

            if (ourElementIsValueType)
                return (T) ourCachedInstanceCtor.Invoke(new object[]
                    {IL2CPP.il2cpp_value_box(Il2CppClassPointerStore<T>.NativeClassPtr, memberPointer)});

            var referencePointer = *(IntPtr*) memberPointer;
            if (referencePointer == IntPtr.Zero) return null;
            
            return (T) ourCachedInstanceCtor.Invoke(new object[] {referencePointer});
        }

        private static IntPtr AllocateArray(long size)
        {
            if(size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Array size must not be negative");

            var elementTypeClassPointer = Il2CppClassPointerStore<T>.NativeClassPtr;
            if(elementTypeClassPointer == IntPtr.Zero)
                throw new ArgumentException($"{nameof(Il2CppReferenceArray<T>)} requires an Il2Cpp reference type, which {typeof(T)} isn't");
            return IL2CPP.il2cpp_array_new(elementTypeClassPointer, (ulong) size);
        }
    }
}