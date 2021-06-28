using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Image
{
    [ApplicableToUnityVersionsSince("2018.3.0")]
    public unsafe class NativeImageStructHandler_24_1 : INativeImageStructHandler
    {
        public INativeImageStruct CreateNewImageStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImage_24_1>());

            *(Il2CppImage_24_1*)pointer = default;

            return new NativeImageStruct(pointer);
        }

        public INativeImageStruct Wrap(Il2CppImage* imagePointer)
        {
            if ((IntPtr)imagePointer == IntPtr.Zero) return null;
            else return new NativeImageStruct((IntPtr)imagePointer);
        }

#if DEBUG
        public string GetName() => "NativeImageStructHandler_24_1";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppImage_24_1
        {
            public IntPtr name; // const char*
            public IntPtr nameNoExt; // const char*
            public Il2CppAssembly* assembly;

            public /*TypeDefinitionIndex*/ int typeStart;
            public uint typeCount;

            public /*TypeDefinitionIndex*/ int exportedTypeStart;
            public uint exportedTypeCount;

            public /*CustomAttributeIndex*/ int customAttributeStart;
            public uint customAttributeCount;

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

            private Il2CppImage_24_1* NativeImage => (Il2CppImage_24_1*)Pointer;

            public ref Il2CppAssembly* Assembly => ref NativeImage->assembly;

            public ref byte Dynamic => ref NativeImage->dynamic;

            public ref IntPtr Name => ref NativeImage->name;

            public bool HasNameNoExt => true;

            public ref IntPtr NameNoExt => ref NativeImage->nameNoExt;
        }
    }
}