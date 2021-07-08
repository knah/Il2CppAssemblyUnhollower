using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.PropertyInfo
{
    public interface INativePropertyInfoStructHandler : INativeStructHandler
    {
        INativePropertyInfoStruct CreateNewPropertyInfoStruct();
        unsafe INativePropertyInfoStruct Wrap(Il2CppPropertyInfo* propertyInfoPointer);
        uint il2cpp_property_get_flags(IntPtr prop);
        IntPtr il2cpp_property_get_name(IntPtr prop);
        IntPtr il2cpp_property_get_parent(IntPtr prop);
        IntPtr il2cpp_property_get_get_method(IntPtr prop);
        IntPtr il2cpp_property_get_set_method(IntPtr prop);
        IntPtr il2cpp_property_get_type(IntPtr prop);
#if DEBUG
        string GetName();
#endif
    }

    public interface INativePropertyInfoStruct : INativeStruct
    {
        unsafe Il2CppPropertyInfo* PropertyInfoPointer { get; }

        ref IntPtr Name { get; }

        unsafe ref Il2CppClass* Parent { get; }

        unsafe ref Il2CppMethodInfo* Get { get; }

        unsafe ref Il2CppMethodInfo* Set { get; }

        ref uint Attrs { get; }
    }
}
