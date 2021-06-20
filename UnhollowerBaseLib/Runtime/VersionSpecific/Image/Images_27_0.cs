using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Image
{
    [ApplicableToUnityVersionsSince("2020.2.0")]
    public unsafe class NativeImageStructHandler_27_0 : INativeImageStructHandler
    {
        public INativeImageStruct CreateNewImageStruct()
        {
            var pointer = (Il2CppImage_27_0*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImage_27_0>());
            var metadataPointer = (Il2CppImageGlobalMetadata_27_0*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImageGlobalMetadata_27_0>());

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

#if DEBUG
        public string GetName() => "NativeImageStructHandler_27_0";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppImage_27_0
        {
            public IntPtr name; // const char*
            public IntPtr nameNoExt; // const char*
            public Il2CppAssembly* assembly;

            public uint typeCount;

            public uint exportedTypeCount;
            public uint customAttributeCount;

            public Il2CppImageGlobalMetadata_27_0* metadataHandle;

            public /*Il2CppNameToTypeDefinitionIndexHashTable **/ IntPtr nameToClassHashTable;
            public IntPtr codeGenModule;

            public uint token;
            public byte dynamic;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppImageGlobalMetadata_27_0
        {
            public int typeStart;
            public int exportedTypeStart;
            public int customAttributeStart;
            public int entryPointIndex;
            public Il2CppImage_27_0* image;
        }

        internal class NativeImageStruct : INativeImageStruct
        {
            public NativeImageStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppImage* ImagePointer => (Il2CppImage*)Pointer;

            private Il2CppImage_27_0* NativeImage => (Il2CppImage_27_0*)Pointer;

            public ref Il2CppAssembly* Assembly => ref NativeImage->assembly;

            public ref byte Dynamic => ref NativeImage->dynamic;

            public ref IntPtr Name => ref NativeImage->name;

            public bool HasNameNoExt => true;

            public ref IntPtr NameNoExt => ref NativeImage->nameNoExt;
        }
    }
}