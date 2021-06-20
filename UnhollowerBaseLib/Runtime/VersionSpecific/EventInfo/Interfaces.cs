using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.EventInfo
{
    public interface INativeEventInfoStructHandler : INativeStructHandler
    {
        INativeEventInfoStruct CreateNewEventInfoStruct();
        unsafe INativeEventInfoStruct Wrap(Il2CppEventInfo* eventInfoPointer);
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
