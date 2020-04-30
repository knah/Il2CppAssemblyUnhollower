using System;
using System.Collections;
using System.Collections.Generic;

namespace UnhollowerBaseLib
{
    public abstract class Il2CppArrayBase<T> : Il2CppObjectBase, IList<T>
    {
        protected Il2CppArrayBase(IntPtr pointer) : base(pointer)
        {
        }

        public int Length => (int) IL2CPP.il2cpp_array_length(Pointer);

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new IndexEnumerator(this);
        }

        void ICollection<T>.Add(T item) => ThrowImmutableLength();

        private static bool ThrowImmutableLength() => throw new NotSupportedException("Arrays have immutable length");

        void ICollection<T>.Clear() => ThrowImmutableLength();
        public bool Contains(T item) => IndexOf(item) != -1;
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<T>.Remove(T item) => ThrowImmutableLength();

        public int Count => Length;
        public bool IsReadOnly => false;
        
        public int IndexOf(T item)
        {
            for (var i = 0; i < Length; i++)
                if (Equals(item, this[i]))
                    return i;

            return -1;
        }

        void IList<T>.Insert(int index, T item) => ThrowImmutableLength();
        void IList<T>.RemoveAt(int index) => ThrowImmutableLength();

        public static implicit operator T[](Il2CppArrayBase<T> il2CppArray)
        {
            if (il2CppArray == null)
                return null;

            var arr = new T[il2CppArray.Length];
            for (var i = 0; i < arr.Length; i++)
                arr[i] = il2CppArray[i];
            
            return arr;
        }

        public abstract T this[int index]
        {
            get;
            set;
        }

        public static Il2CppArrayBase<T> WrapNativeGenericArrayPointer(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero) return null;

            if (typeof(T) == typeof(string)) 
                return new Il2CppStringArray(pointer) as Il2CppArrayBase<T>;
            if (typeof(T).IsValueType) // can't construct required types here directly because of unfulfilled generic constraint
                return Activator.CreateInstance(typeof(Il2CppStructArray<>).MakeGenericType(typeof(T)), pointer) as Il2CppArrayBase<T>;
            if (typeof(Il2CppObjectBase).IsAssignableFrom(typeof(T)))
                return Activator.CreateInstance(typeof(Il2CppReferenceArray<>).MakeGenericType(typeof(T)), pointer) as Il2CppArrayBase<T>;
            
            throw new ArgumentException($"{typeof(T)} is not a value type, not a string and not an IL2CPP object; it can't be used in IL2CPP arrays");
        }

        private class IndexEnumerator : IEnumerator<T>
        {
            private Il2CppArrayBase<T> myArray;
            private int myIndex = -1;

            public IndexEnumerator(Il2CppArrayBase<T> array) => myArray = array;
            public void Dispose() => myArray = null;
            public bool MoveNext() => ++myIndex < myArray.Count;
            public void Reset() => myIndex = -1;
            object IEnumerator.Current => Current;
            public T Current => myArray[myIndex];
        }
    }
}