using System;

namespace UnhollowerBaseLib
{
    public interface IIl2CppNullable // non-generic interface for type checks and writes
    {
        public void WriteToStorage(IntPtr pointer);
        public IntPtr WriteForMethodCall();
        public void ReplaceContentsIfNecessary(IntPtr newData);
    }
}
