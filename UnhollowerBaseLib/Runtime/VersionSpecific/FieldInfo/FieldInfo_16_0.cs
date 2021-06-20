﻿using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.FieldInfo
{
    [ApplicableToUnityVersionsSince("5.3.0")]
    public unsafe class NativeFieldInfoStructHandler_16_0 : INativeFieldInfoStructHandler
    {
        public INativeFieldInfoStruct CreateNewFieldInfoStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppFieldInfo_16_0>());

            *(Il2CppFieldInfo_16_0*)pointer = default;

            return new NativeFieldInfoStruct(pointer);
        }

        public INativeFieldInfoStruct Wrap(Il2CppFieldInfo* fieldInfoPointer)
        {
            return new NativeFieldInfoStruct((IntPtr)fieldInfoPointer);
        }

#if DEBUG
        public string GetName() => "NativeFieldInfoStructHandler_16_0";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppFieldInfo_16_0
        {
            public IntPtr name; // const char*
            public Il2CppTypeStruct* type; // const
            public Il2CppClass* parent; // non-const?
            public int offset; // If offset is -1, then it's thread static
            public IntPtr customAttributeIndex;
        }

        internal class NativeFieldInfoStruct : INativeFieldInfoStruct
        {
            public NativeFieldInfoStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppFieldInfo* FieldInfoPointer => (Il2CppFieldInfo*)Pointer;

            private Il2CppFieldInfo_16_0* NativeField => (Il2CppFieldInfo_16_0*)Pointer;

            public ref IntPtr Name => ref NativeField->name;

            public ref Il2CppTypeStruct* Type => ref NativeField->type;

            public ref Il2CppClass* Parent => ref NativeField->parent;

            public ref int Offset => ref NativeField->offset;
        }
    }
}