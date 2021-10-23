using System;

namespace UnhollowerBaseLib
{
    /// <summary>
    /// non-generic interface for type checks and writes
    /// </summary>
    public interface IIl2CppNullable
    {
        public void WriteToStorage(IntPtr pointer);
        public IntPtr WriteForMethodCall();
        public void ReplaceContentsIfNecessary(IntPtr newData);
    }
}
