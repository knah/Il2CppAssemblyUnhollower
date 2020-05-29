using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific
{
    public class Unity2018NativeClassStructHandler : INativeClassStructHandler
    {
        public unsafe INativeClassStruct CreateNewClassStruct(int vTableSlots)
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppClassU2018>() + Marshal.SizeOf<VirtualInvokeData>() * vTableSlots);

            *(Il2CppClassU2018*) pointer = default;
            
            return new Unity2018NativeClassStruct(pointer);
        }

        public unsafe INativeClassStruct Wrap(Il2CppClass* classPointer)
        {
            return new Unity2018NativeClassStruct((IntPtr) classPointer);
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppClassU2018
        {
            public Il2CppClassPart1 Part1;
            public Il2CppClassPart2 Part2;
        }

        private unsafe class Unity2018NativeClassStruct : INativeClassStruct
        {
            public Unity2018NativeClassStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }
            public Il2CppClass* ClassPointer => (Il2CppClass*) Pointer;

            public IntPtr VTable => IntPtr.Add(Pointer, Marshal.SizeOf<Il2CppClassU2018>());

            public Il2CppClassPart1* Part1 => &((Il2CppClassU2018*) Pointer)->Part1;
            public Il2CppClassPart2* Part2 => &((Il2CppClassU2018*) Pointer)->Part2;
        }
    }
}