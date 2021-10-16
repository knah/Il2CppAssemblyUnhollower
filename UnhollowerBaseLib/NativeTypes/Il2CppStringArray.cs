using System;

namespace UnhollowerBaseLib
{
    /// <summary>
    /// todo: remove
    /// </summary>
    public class Il2CppStringArray : Il2CppArrayBase<string>
    {
        public Il2CppStringArray(IntPtr pointer) : base(pointer)
        {
        }
        
        public Il2CppStringArray(long size) : base(AllocateArray(size))
        {
        }

        public Il2CppStringArray(string[] arr) : base(AllocateArray(arr.Length))
        {
            for (var i = 0; i < arr.Length; i++) 
                this[i] = arr[i];
        }

        static Il2CppStringArray()
        {
            StaticCtorBody(typeof(Il2CppStringArray));
        }
        
        public static implicit operator Il2CppStringArray(string[] arr)
        {
            if (arr == null) return null;
            
            return new Il2CppStringArray(arr);
        }
        
        private static IntPtr AllocateArray(long size)
        {
            if(size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Array size must not be negative");

            var elementTypeClassPointer = Il2CppClassPointerStore<string>.NativeClassPtr;
            if (elementTypeClassPointer == IntPtr.Zero)
                throw new ArgumentException("String class pointer is missing, something is very wrong");
            return IL2CPP.il2cpp_array_new(elementTypeClassPointer, (ulong) size);
        }

        public override unsafe string this[int index]
        {
            get
            {
                if(index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Array index may not be negative or above length of the array");
                var arrayStartPointer = IntPtr.Add(Pointer, 4 * IntPtr.Size);
                var elementPointer = IntPtr.Add(arrayStartPointer, index * IntPtr.Size);
                return IL2CPP.Il2CppStringToManaged(*(IntPtr*) elementPointer);
            }
            set
            {
                if(index < 0 || index >= Length)
                    throw new ArgumentOutOfRangeException(nameof(index), "Array index may not be negative or above length of the array");
                var arrayStartPointer = IntPtr.Add(Pointer, 4 * IntPtr.Size);
                var elementPointer = IntPtr.Add(arrayStartPointer, index * IntPtr.Size);
                *(IntPtr*)elementPointer = IL2CPP.ManagedStringToIl2Cpp(value);
            }
        }
    }
}