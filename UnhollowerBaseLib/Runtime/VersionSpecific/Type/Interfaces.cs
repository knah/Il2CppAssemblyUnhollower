using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Type
{
    public interface INativeTypeStructHandler : INativeStructHandler
    {
        INativeTypeStruct CreateNewTypeStruct();
        unsafe INativeTypeStruct Wrap(Il2CppTypeStruct* imagePointer);
    }

    public interface INativeTypeStruct : INativeStruct
    {
        unsafe Il2CppTypeStruct* TypePointer { get; }

        ref Il2CppTypeEnum Type { get; }

        ref IntPtr Data { get; }
    }
}
