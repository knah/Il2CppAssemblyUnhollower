using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.FieldInfo
{
    [ApplicableToUnityVersionsSince("2017.1.0")]
    public unsafe class NativeFieldInfoStructHandler_24_0 : INativeFieldInfoStructHandler
    {
        public INativeFieldInfoStruct CreateNewFieldInfoStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppFieldInfo_24_0>());

            *(Il2CppFieldInfo_24_0*)pointer = default;

            return new NativeFieldInfoStruct(pointer);
        }

        public INativeFieldInfoStruct Wrap(Il2CppFieldInfo* fieldInfoPointer)
        {
            return new NativeFieldInfoStruct((IntPtr)fieldInfoPointer);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppFieldInfo_24_0
        {
            public IntPtr name; // const char*
            public Il2CppTypeStruct* type; // const
            public Il2CppClass* parent; // non-const?
            public int offset; // If offset is -1, then it's thread static
            public IntPtr customAttributeIndex;
            public uint token;
        }

        private class NativeFieldInfoStruct : INativeFieldInfoStruct
        {
            public NativeFieldInfoStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppFieldInfo* FieldInfoPointer => (Il2CppFieldInfo*)Pointer;

            private Il2CppFieldInfo_24_0* NativeField => (Il2CppFieldInfo_24_0*)Pointer;

            public ref IntPtr Name => ref NativeField->name;

            public ref Il2CppTypeStruct* Type => ref NativeField->type;

            public ref Il2CppClass* Parent => ref NativeField->parent;

            public ref int Offset => ref NativeField->offset;

            public ref uint Token => ref NativeField->token;
        }
    }
}
