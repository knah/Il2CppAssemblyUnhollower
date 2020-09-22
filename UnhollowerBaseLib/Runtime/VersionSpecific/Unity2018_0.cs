using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific
{
    public class Unity2018_0NativeClassStructHandler : INativeClassStructHandler
    {
        public unsafe INativeClassStruct CreateNewClassStruct(int vTableSlots)
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppClassU2018_0>() + Marshal.SizeOf<VirtualInvokeData>() * vTableSlots);

            *(Il2CppClassU2018_0*) pointer = default;
            
            return new Unity2018_0NativeClassStruct(pointer);
        }

        public unsafe INativeClassStruct Wrap(Il2CppClass* classPointer)
        {
            return new Unity2018_0NativeClassStruct((IntPtr) classPointer);
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppClassU2018_0
        {
            public Il2CppClassPart1 Part1;
            public Il2CppClassPart2 Part2;
            public byte typeHierarchyDepth; // Initialized in SetupTypeHierachy
            public byte genericRecursionDepth;
            public byte rank;
            public byte minimumAlignment; // Alignment of this type
            public byte packingSize;
            public ClassBitfield1 bitfield_1;
            public ClassBitfield2 bitfield_2;
        }

        private unsafe class Unity2018_0NativeClassStruct : INativeClassStruct
        {
            public Unity2018_0NativeClassStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }
            public Il2CppClass* ClassPointer => (Il2CppClass*) Pointer;

            public IntPtr VTable => IntPtr.Add(Pointer, Marshal.SizeOf<Il2CppClassU2018_0>());

            public Il2CppClassPart1* Part1 => &((Il2CppClassU2018_0*) Pointer)->Part1;
            public Il2CppClassPart2* Part2 => &((Il2CppClassU2018_0*) Pointer)->Part2;
            public ClassBitfield1* Bitfield1 => &((Il2CppClassU2018_0*)Pointer)->bitfield_1;
            public ClassBitfield2* Bitfield2 => &((Il2CppClassU2018_0*)Pointer)->bitfield_2;
        }
    }
}