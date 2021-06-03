using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Image
{
    [ApplicableToUnityVersionsSince("2020.2.0")]
    public unsafe class NativeImageStructHandler_27 : INativeImageStructHandler
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

            public uint typeCount;

            public uint exportedTypeCount;
            public uint customAttributeCount;

            public IntPtr metadataHandle;

            public /*Il2CppNameToTypeDefinitionIndexHashTable **/ IntPtr nameToClassHashTable;
            public IntPtr codeGenModule;

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