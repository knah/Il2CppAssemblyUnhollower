using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.FieldInfo
{
    [ApplicableToUnityVersionsSince("2018.3.0")]
    public unsafe class NativeFieldInfoStructHandler_24_1 : INativeFieldInfoStructHandler
    {
        public INativeFieldInfoStruct CreateNewFieldInfoStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppFieldInfo_24_1>());

            *(Il2CppFieldInfo_24_1*)pointer = default;

            return new NativeFieldInfoStruct(pointer);
        }

        public INativeFieldInfoStruct Wrap(Il2CppFieldInfo* fieldInfoPointer)
        {
            if ((IntPtr)fieldInfoPointer == IntPtr.Zero) return null;
            else return new NativeFieldInfoStruct((IntPtr)fieldInfoPointer);
        }

        public IntPtr il2cpp_field_get_name(IntPtr field) => ((Il2CppFieldInfo_24_1*)field)->name;
        public int il2cpp_field_get_offset(IntPtr field) => ((Il2CppFieldInfo_24_1*)field)->offset;
        public IntPtr il2cpp_field_get_parent(IntPtr field) => (IntPtr)((Il2CppFieldInfo_24_1*)field)->parent;
        public IntPtr il2cpp_field_get_type(IntPtr field) => (IntPtr)((Il2CppFieldInfo_24_1*)field)->type;

#if DEBUG
        public string GetName() => "NativeFieldInfoStructHandler_24_1";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppFieldInfo_24_1
        {
            public IntPtr name; // const char*
            public Il2CppTypeStruct* type; // const
            public Il2CppClass* parent; // non-const?
            public int offset; // If offset is -1, then it's thread static
            public uint token;
        }

        internal class NativeFieldInfoStruct : INativeFieldInfoStruct
        {
            public NativeFieldInfoStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppFieldInfo* FieldInfoPointer => (Il2CppFieldInfo*)Pointer;

            private Il2CppFieldInfo_24_1* NativeField => (Il2CppFieldInfo_24_1*)Pointer;

            public ref IntPtr Name => ref NativeField->name;

            public ref Il2CppTypeStruct* Type => ref NativeField->type;

            public ref Il2CppClass* Parent => ref NativeField->parent;

            public ref int Offset => ref NativeField->offset;
        }
    }
}
