using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.FieldInfo
{
    public interface INativeFieldInfoStructHandler : INativeStructHandler
    {
        INativeFieldInfoStruct CreateNewFieldInfoStruct();
        unsafe INativeFieldInfoStruct Wrap(Il2CppFieldInfo* fieldInfoPointer);
    }

    public interface INativeFieldInfoStruct : INativeStruct
    {
        unsafe Il2CppFieldInfo* FieldInfoPointer { get; }

        ref IntPtr Name { get; }

        unsafe ref Il2CppTypeStruct* Type { get; }

        unsafe ref Il2CppClass* Parent { get; }

        ref int Offset { get; }
    }
}
