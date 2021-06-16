using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Image
{
    [ApplicableToUnityVersionsSince("2018.1.0")]
    public unsafe class NativeImageStructHandler_24_0_C : INativeImageStructHandler
    {
        public INativeImageStruct CreateNewImageStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImage_24_0_C>());

            *(Il2CppImage_24_0_C*)pointer = default;

            return new NativeImageStruct(pointer);
        }

        public INativeImageStruct Wrap(Il2CppImage* imagePointer)
        {
            return new NativeImageStruct((IntPtr)imagePointer);
        }

        public string GetName() => "NativeImageStructHandler_24_0_C";

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppImage_24_0_C
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

        internal class NativeImageStruct : INativeImageStruct
        {
            public NativeImageStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppImage* ImagePointer => (Il2CppImage*)Pointer;

            private Il2CppImage_24_0_C* NativeImage => (Il2CppImage_24_0_C*)Pointer;

            public ref Il2CppAssembly* Assembly => ref NativeImage->assembly;

            public ref byte Dynamic => ref NativeImage->dynamic;

            public ref IntPtr Name => ref NativeImage->name;

            public bool HasNameNoExt => true;

            public ref IntPtr NameNoExt => ref NativeImage->nameNoExt;
        }
    }
}