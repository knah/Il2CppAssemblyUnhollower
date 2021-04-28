using System;

namespace UnhollowerBaseLib
{
    public class Il2CppStructArray<T> : Il2CppArrayBase<T> where T: unmanaged
    {
        public Il2CppStructArray(IntPtr nativeObject) : base(nativeObject)
        {
        }

        public Il2CppStructArray(long size) : base(AllocateArray(size))
        {
        }

        public static implicit operator Il2CppStructArray<T>(T[] arr)
        {
            if (arr == null) return null;
            
            var il2CppArray = new Il2CppStructArray<T>(arr.Length);
            for (var i = 0; i < arr.Length; i++) il2CppArray[i] = arr[i];

            return il2CppArray;
        }

        public override unsafe T this[int index]
        {
            get
            {
                if(index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Array index may not be negative or above length of the array");
                var arrayStartPointer = IntPtr.Add(Pointer, 4 * IntPtr.Size);
                return ((T*) arrayStartPointer.ToPointer())[index];
            }
            set
            {
                if(index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Array index may not be negative or above length of the array");
                var arrayStartPointer = IntPtr.Add(Pointer, 4 * IntPtr.Size);
                ((T*) arrayStartPointer.ToPointer())[index] = value;
            }
        }
        
        private static IntPtr AllocateArray(long size)
        {
            if(size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Array size must not be negative");

            var elementTypeClassPointer = Il2CppClassPointerStore<T>.NativeClassPtr;
            if(elementTypeClassPointer == IntPtr.Zero)
                throw new ArgumentException($"{nameof(Il2CppStructArray<T>)} requires an Il2Cpp reference type, which {typeof(T)} isn't");
            return IL2CPP.il2cpp_array_new(elementTypeClassPointer, (ulong) size);
        }
    }
}