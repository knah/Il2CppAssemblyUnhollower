using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Image
{
    [ApplicableToUnityVersionsSince("2020.2.0")]
    public unsafe class NativeImageStructHandler_27 : INativeImageStructHandler
    {
        public INativeImageStruct CreateNewImageStruct()
        {
            var pointer = (Il2CppImageU2019*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImageU2019>());
            var metadataPointer = (Il2CppImageGlobalMetadata*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImageGlobalMetadata>());

            *pointer = default;
            *metadataPointer = default;
            pointer->metadataHandle = metadataPointer;
            metadataPointer->image = pointer;

            return new NativeImageStruct((IntPtr) pointer);
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

            public Il2CppImageGlobalMetadata* metadataHandle;

            public /*Il2CppNameToTypeDefinitionIndexHashTable **/ IntPtr nameToClassHashTable;
            public IntPtr codeGenModule;

            public uint token;
            public byte dynamic;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppImageGlobalMetadata
        {
            public int typeStart;
            public int exportedTypeStart;
            public int customAttributeStart;
            public int entryPointIndex;
            public Il2CppImageU2019* image;
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