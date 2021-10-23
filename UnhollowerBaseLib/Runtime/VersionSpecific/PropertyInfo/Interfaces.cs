using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.PropertyInfo
{
    public interface INativePropertyInfoStructHandler : INativeStructHandler
    {
        INativePropertyInfoStruct CreateNewPropertyInfoStruct();
        unsafe INativePropertyInfoStruct Wrap(Il2CppPropertyInfo* propertyInfoPointer);
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
