using System;

namespace UnhollowerBaseLib.Runtime
{
    public interface INativeClassStruct
    {
        IntPtr Pointer { get; }
        unsafe Il2CppClass* ClassPointer { get; }
        IntPtr VTable { get; }

        unsafe Il2CppClassPart1* Part1 { get; }
        unsafe Il2CppClassPart2* Part2 { get; }
        unsafe ClassBitfield1* Bitfield1 { get; }
        unsafe ClassBitfield2* Bitfield2 { get; }
    }
}