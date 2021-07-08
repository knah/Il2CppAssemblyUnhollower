using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.EventInfo
{
    public interface INativeEventInfoStructHandler : INativeStructHandler
    {
        INativeEventInfoStruct CreateNewEventInfoStruct();
        unsafe INativeEventInfoStruct Wrap(Il2CppEventInfo* eventInfoPointer);
        IntPtr il2cpp_event_get_name(IntPtr eventPointer);
        IntPtr il2cpp_event_get_type(IntPtr eventPointer);
        IntPtr il2cpp_event_get_parent_class(IntPtr eventPointer);
        IntPtr il2cpp_event_get_add_method(IntPtr eventPointer);
        IntPtr il2cpp_event_get_remove_method(IntPtr eventPointer);
        IntPtr il2cpp_event_get_raise_method(IntPtr eventPointer);
#if DEBUG
        string GetName();
#endif
    }

    public interface INativeEventInfoStruct : INativeStruct
    {
        unsafe Il2CppEventInfo* EventInfoPointer { get; }

        ref IntPtr Name { get; }

        unsafe ref Il2CppTypeStruct* EventType { get; }

        unsafe ref Il2CppClass* Parent { get; }

        unsafe ref Il2CppMethodInfo* Add { get; }

        unsafe ref Il2CppMethodInfo* Remove { get; }

        unsafe ref Il2CppMethodInfo* Raise { get; }
    }
}
