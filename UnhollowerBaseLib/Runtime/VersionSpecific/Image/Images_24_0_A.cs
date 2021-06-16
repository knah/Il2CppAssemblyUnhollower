using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Image
{
    [ApplicableToUnityVersionsSince("2017.1.0")]
    [ApplicableToUnityVersionsSince("2017.2.0")]
    public unsafe class NativeImageStructHandler_24_0_A : INativeImageStructHandler
    {
        public INativeImageStruct CreateNewImageStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImage_24_0_A>());

            *(Il2CppImage_24_0_A*)pointer = default;

            return new NativeImageStruct(pointer);
        }

        public INativeImageStruct Wrap(Il2CppImage* imagePointer)
        {
            return new NativeImageStruct((IntPtr)imagePointer);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppImage_24_0_A
        {
            public IntPtr name;      // const char*
            public /*AssemblyIndex*/ int assemblyIndex;

            public /*TypeDefinitionIndex*/ int typeStart;
            public uint typeCount;

            public /*TypeDefinitionIndex*/ int exportedTypeStart;
            public uint exportedTypeCount;
            
            public /*MethodIndex*/ int entryPointIndex;

            public /*Il2CppNameToTypeDefinitionIndexHashTable **/ IntPtr nameToClassHashTable;

            public uint token;
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

            private Il2CppImage_24_0_A* NativeImage => (Il2CppImage_24_0_A*)Pointer;

            public ref Il2CppAssembly* Assembly => throw new NotSupportedException();

            public ref byte Dynamic => ref dynamicDummy;

            public ref IntPtr Name => ref NativeImage->name;

            public bool HasNameNoExt => false;

            public ref IntPtr NameNoExt => throw new NotSupportedException();
        }
    }
}