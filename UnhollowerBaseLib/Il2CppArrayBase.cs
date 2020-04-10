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

        public void Add(T item) => ThrowImmutableLength();

        private static bool ThrowImmutableLength() => throw new NotSupportedException("Arrays have immutable length");

        public void Clear() => ThrowImmutableLength();
        public bool Contains(T item) => IndexOf(item) != -1;
        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item) => ThrowImmutableLength();

        public int Count => Length;
        public bool IsReadOnly => false;
        
        public int IndexOf(T item)
        {
            for (var i = 0; i < Length; i++)
                if (Equals(item, this[i]))
                    return i;

            return -1;
        }

        public void Insert(int index, T item) => ThrowImmutableLength();
        public void RemoveAt(int index) => ThrowImmutableLength();

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