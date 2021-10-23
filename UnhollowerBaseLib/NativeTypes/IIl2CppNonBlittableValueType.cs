using System;

namespace UnhollowerBaseLib
{
    public interface IIl2CppNonBlittableValueType : IIl2CppObjectBase
    {
        Span<byte> ObjectBytes { get; }
        IntPtr ObjectBytesPointer { get; }
    }
}
