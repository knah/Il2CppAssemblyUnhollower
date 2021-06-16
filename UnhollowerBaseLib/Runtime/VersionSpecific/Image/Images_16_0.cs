using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Image
{
    [ApplicableToUnityVersionsSince("5.3.0")]
    public unsafe class NativeImageStructHandler_16_0 : INativeImageStructHandler
    {
        public INativeImageStruct CreateNewImageStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImage_16_0>());

            *(Il2CppImage_16_0*)pointer = default;

            return new NativeImageStruct(pointer);
        }

        public INativeImageStruct Wrap(Il2CppImage* imagePointer)
        {
            return new NativeImageStruct((IntPtr)imagePointer);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppImage_16_0
        {
            public IntPtr name;      // const char*
            public /*AssemblyIndex*/ int assemblyIndex;

            public /*TypeDefinitionIndex*/ int typeStart;
            public uint typeCount;

            public /*MethodIndex*/ int entryPointIndex;

            public /*Il2CppNameToTypeDefinitionIndexHashTable **/ IntPtr nameToClassHashTable;
        }

        internal class NativeImageStruct : INativeImageStruct
        {
            private static byte dynamicDummy;

            public NativeImageStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppImage* ImagePointer => (Il2CppImage*)Pointer;

            private Il2CppImage_16_0* NativeImage => (Il2CppImage_16_0*)Pointer;

            public ref Il2CppAssembly* Assembly => throw new NotSupportedException();

            public ref byte Dynamic => ref dynamicDummy;

            public ref IntPtr Name => ref NativeImage->name;

            public bool HasNameNoExt => false;

            public ref IntPtr NameNoExt => throw new NotSupportedException();
        }
    }
}