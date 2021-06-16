using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.PropertyInfo
{
    [ApplicableToUnityVersionsSince("5.3.0")]
    public unsafe class NativePropertyInfoStructHandler_16_0 : INativePropertyInfoStructHandler
    {
        public INativePropertyInfoStruct CreateNewPropertyInfoStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppPropertyInfo_16_0>());

            *(Il2CppPropertyInfo_16_0*)pointer = default;

            return new NativePropertyInfoStruct(pointer);
        }

        public INativePropertyInfoStruct Wrap(Il2CppPropertyInfo* propertyInfoPointer)
        {
            return new NativePropertyInfoStruct((IntPtr)propertyInfoPointer);
        }

        public string GetName() => "NativePropertyInfoStructHandler_16_0";

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppPropertyInfo_16_0
        {
            public Il2CppClass* parent;
            public IntPtr name; // const char*
            public Il2CppMethodInfo* get; // const
            public Il2CppMethodInfo* set; // const
            public uint attrs;
            public IntPtr customAttributeIndex;
        }

        internal class NativePropertyInfoStruct : INativePropertyInfoStruct
        {
            public NativePropertyInfoStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppPropertyInfo* PropertyInfoPointer => (Il2CppPropertyInfo*)Pointer;

            private Il2CppPropertyInfo_16_0* NativeProperty => (Il2CppPropertyInfo_16_0*)Pointer;

            public ref IntPtr Name => ref NativeProperty->name;

            public ref Il2CppClass* Parent => ref NativeProperty->parent;

            public ref Il2CppMethodInfo* Get => ref NativeProperty->get;

            public ref Il2CppMethodInfo* Set => ref NativeProperty->set;

            public ref uint Attrs => ref NativeProperty->attrs;
        }
    }
}
