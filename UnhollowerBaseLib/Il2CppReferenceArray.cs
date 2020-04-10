using System;
using System.Reflection;

namespace UnhollowerBaseLib
{
    public class Il2CppReferenceArray<T> : Il2CppArrayBase<T> where T: Il2CppObjectBase
    {
        private static ConstructorInfo ourCachedInstanceCtor;
        
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
        
        public static implicit operator Il2CppReferenceArray<T>(T[] arr)
        {
            if (arr == null) return null;
            
            return new Il2CppReferenceArray<T>(arr);
        }

        public override unsafe T this[int index]
        {
            get
            {
                if(index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Array index may not be negative or above length of the array");
                var arrayStartPointer = IntPtr.Add(Pointer, 4 * IntPtr.Size);
                return WrapElement(((IntPtr*) arrayStartPointer.ToPointer())[index]);
            }
            set
            {
                if(index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Array index may not be negative or above length of the array");
                var arrayStartPointer = IntPtr.Add(Pointer, 4 * IntPtr.Size);
                ((IntPtr*) arrayStartPointer.ToPointer())[index] = value.Pointer;
            }
        }

        private static T WrapElement(IntPtr member)
        {
            if (ourCachedInstanceCtor == null)
            {
                ourCachedInstanceCtor = typeof(T).GetConstructor(new[] {typeof(IntPtr)});
            }

            return (T) ourCachedInstanceCtor.Invoke(new object[] {member});
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