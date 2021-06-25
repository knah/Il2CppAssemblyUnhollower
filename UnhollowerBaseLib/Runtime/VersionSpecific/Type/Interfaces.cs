using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Type
{
    public interface INativeTypeStructHandler : INativeStructHandler
    {
        INativeTypeStruct CreateNewTypeStruct();
        unsafe INativeTypeStruct Wrap(Il2CppTypeStruct* typePointer);
#if DEBUG
        string GetName();
#endif
    }

    public interface INativeTypeStruct : INativeStruct
    {
        unsafe Il2CppTypeStruct* TypePointer { get; }

        ref IntPtr Data { get; }

        ref Il2CppTypeEnum Type { get; }

        bool ByRef { get; set; }

        bool Pinned { get; set; }
    }
}
