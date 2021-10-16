using System;

namespace UnhollowerBaseLib
{
    /// <summary>
    /// todo: generate non-blittable types inherited from this
    /// </summary>
    public class Il2CppNonBlittableValueType : Il2CppSystem.ValueType, IIl2CppNonBlittableValueType
    {
        private readonly int mySize;

        public Il2CppNonBlittableValueType(IntPtr boxedPointer, int size) : base(boxedPointer)
        {
            mySize = size;
        }

        public unsafe Span<byte> ObjectBytes => new((void*)ObjectBytesPointer, mySize);
        public IntPtr ObjectBytesPointer => Pointer + IntPtr.Size * 2;
    }
}
