using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.FieldInfo
{
    public interface INativeFieldInfoStructHandler : INativeStructHandler
    {
        INativeFieldInfoStruct CreateNewFieldInfoStruct();
        unsafe INativeFieldInfoStruct Wrap(Il2CppFieldInfo* fieldInfoPointer);
        IntPtr il2cpp_field_get_name(IntPtr field);
        int il2cpp_field_get_offset(IntPtr field);
        IntPtr il2cpp_field_get_parent(IntPtr field);
        IntPtr il2cpp_field_get_type(IntPtr field);
#if DEBUG
        string GetName();
#endif
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
