using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.EventInfo
{
    [ApplicableToUnityVersionsSince("2018.3.0")]
    public unsafe class NativeEventInfoStructHandler_24_1 : INativeEventInfoStructHandler
    {
        public INativeEventInfoStruct CreateNewEventInfoStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppEventInfo_24_1>());

            *(Il2CppEventInfo_24_1*)pointer = default;

            return new NativeEventInfoStruct(pointer);
        }

        public INativeEventInfoStruct Wrap(Il2CppEventInfo* eventInfoPointer)
        {
            return new NativeEventInfoStruct((IntPtr)eventInfoPointer);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppEventInfo_24_1
        {
            public IntPtr name; // const char*
            public Il2CppTypeStruct* eventType; // const
            public Il2CppClass* parent; // non const
            public Il2CppMethodInfo* add; // const
            public Il2CppMethodInfo* remove; // const
            public Il2CppMethodInfo* raise; // const
            public uint token;
        }

        private class NativeEventInfoStruct : INativeEventInfoStruct
        {
            public NativeEventInfoStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppEventInfo* EventInfoPointer => (Il2CppEventInfo*)Pointer;

            private Il2CppEventInfo_24_1* NativeEvent => (Il2CppEventInfo_24_1*)Pointer;

            public ref IntPtr Name => ref NativeEvent->name;

            public ref Il2CppTypeStruct* EventType => ref NativeEvent->eventType;

            public ref Il2CppClass* Parent => ref NativeEvent->parent;

            public ref Il2CppMethodInfo* Add => ref NativeEvent->add;

            public ref Il2CppMethodInfo* Remove => ref NativeEvent->remove;

            public ref Il2CppMethodInfo* Raise => ref NativeEvent->raise;

            public ref uint Token => ref NativeEvent->token;
        }
    }
}
