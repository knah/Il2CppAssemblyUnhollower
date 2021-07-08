﻿using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.EventInfo
{
    [ApplicableToUnityVersionsSince("5.3.0")]
    public unsafe class NativeEventInfoStructHandler_16_0 : INativeEventInfoStructHandler
    {
        public INativeEventInfoStruct CreateNewEventInfoStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppEventInfo_16_0>());

            *(Il2CppEventInfo_16_0*)pointer = default;

            return new NativeEventInfoStruct(pointer);
        }

        public INativeEventInfoStruct Wrap(Il2CppEventInfo* eventInfoPointer)
        {
            if ((IntPtr)eventInfoPointer == IntPtr.Zero) return null;
            else return new NativeEventInfoStruct((IntPtr)eventInfoPointer);
        }

        public IntPtr il2cpp_event_get_name(IntPtr eventPointer) => ((Il2CppEventInfo_16_0*)eventPointer)->name;
        public IntPtr il2cpp_event_get_type(IntPtr eventPointer) => (IntPtr)((Il2CppEventInfo_16_0*)eventPointer)->eventType;
        public IntPtr il2cpp_event_get_parent_class(IntPtr eventPointer) => (IntPtr)((Il2CppEventInfo_16_0*)eventPointer)->parent;
        public IntPtr il2cpp_event_get_add_method(IntPtr eventPointer) => (IntPtr)((Il2CppEventInfo_16_0*)eventPointer)->add;
        public IntPtr il2cpp_event_get_remove_method(IntPtr eventPointer) => (IntPtr)((Il2CppEventInfo_16_0*)eventPointer)->remove;
        public IntPtr il2cpp_event_get_raise_method(IntPtr eventPointer) => (IntPtr)((Il2CppEventInfo_16_0*)eventPointer)->raise;

#if DEBUG
        public string GetName() => "NativeEventInfoStructHandler_16_0";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppEventInfo_16_0
        {
            public IntPtr name; // const char*
            public Il2CppTypeStruct* eventType; // const
            public Il2CppClass* parent; // non const
            public Il2CppMethodInfo* add; // const
            public Il2CppMethodInfo* remove; // const
            public Il2CppMethodInfo* raise; // const
            public int customAttributeIndex;
        }

        internal class NativeEventInfoStruct : INativeEventInfoStruct
        {
            public NativeEventInfoStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppEventInfo* EventInfoPointer => (Il2CppEventInfo*)Pointer;

            private Il2CppEventInfo_16_0* NativeEvent => (Il2CppEventInfo_16_0*)Pointer;

            public ref IntPtr Name => ref NativeEvent->name;

            public ref Il2CppTypeStruct* EventType => ref NativeEvent->eventType;

            public ref Il2CppClass* Parent => ref NativeEvent->parent;

            public ref Il2CppMethodInfo* Add => ref NativeEvent->add;

            public ref Il2CppMethodInfo* Remove => ref NativeEvent->remove;

            public ref Il2CppMethodInfo* Raise => ref NativeEvent->raise;
        }
    }
}
