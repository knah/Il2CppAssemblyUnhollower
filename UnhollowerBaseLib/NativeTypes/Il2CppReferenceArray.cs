using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib
{
    public class Il2CppReferenceArray<T> : Il2CppArrayBase<T> where T: Il2CppObjectBase
    {
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
            StaticCtorBody(typeof(Il2CppReferenceArray<T>));
        }
        
        public static implicit operator Il2CppReferenceArray<T>(T[] arr)
        {
            if (arr == null) return null;
            
            return new Il2CppReferenceArray<T>(arr);
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