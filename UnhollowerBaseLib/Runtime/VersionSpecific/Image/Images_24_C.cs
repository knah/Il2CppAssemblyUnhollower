using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Image
{
    [ApplicableToUnityVersionsSince("2018.1.0")]
    public unsafe class NativeImageStructHandler_24_C : INativeImageStructHandler
    {
        public INativeImageStruct CreateNewImageStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImageU2019>());

            *(Il2CppImageU2019*)pointer = default;

            return new NativeImageStruct(pointer);
        }

        public INativeImageStruct Wrap(Il2CppImage* imagePointer)
        {
            return new NativeImageStruct((IntPtr)imagePointer);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppImageU2019
        {
            public IntPtr name; // const char*
            public IntPtr nameNoExt; // const char*
            public Il2CppAssembly* assembly;

            public /*TypeDefinitionIndex*/ int typeStart;
            public uint typeCount;

            public /*TypeDefinitionIndex*/ int exportedTypeStart;
            public uint exportedTypeCount;

            public /*MethodIndex*/ int entryPointIndex;

            public /*Il2CppNameToTypeDefinitionIndexHashTable **/ IntPtr nameToClassHashTable;

            public uint token;
            public byte dynamic;
        }

        private class NativeImageStruct : INativeImageStruct
        {
            public NativeImageStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppImage* ImagePointer => (Il2CppImage*)Pointer;

            private Il2CppImageU2019* NativeImage => (Il2CppImageU2019*)ImagePointer;

            public ref Il2CppAssembly* Assembly => ref NativeImage->assembly;

            public ref byte Dynamic => ref NativeImage->dynamic;

            public ref IntPtr Name => ref NativeImage->name;
            public bool HasNameNoExt => true;

            public ref IntPtr NameNoExt => ref NativeImage->nameNoExt;
        }
    }
}