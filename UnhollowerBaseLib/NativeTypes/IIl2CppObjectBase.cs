using System;

namespace UnhollowerBaseLib
{
    public interface IIl2CppObjectBase
    {
        IntPtr Pointer { get; }
        IntPtr PointerNullable { get; }
        bool IsIl2CppObjectAlive();
    }
}
