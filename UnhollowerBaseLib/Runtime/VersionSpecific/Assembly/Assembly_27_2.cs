using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Assembly
{
    [ApplicableToUnityVersionsSince("2021.1.0")]
    public unsafe class NativeAssemblyStructHandler_27_2 : INativeAssemblyStructHandler
    {
        public INativeAssemblyStruct CreateNewAssemblyStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppAssembly_27_2>());

            *(Il2CppAssembly_27_2*)pointer = default;

            return new NativeAssemblyStruct(pointer);
        }

        public INativeAssemblyStruct Wrap(Il2CppAssembly* assemblyPointer)
        {
            return new NativeAssemblyStruct((IntPtr)assemblyPointer);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppAssembly_27_2
        {
            public Il2CppImage* image;
            public uint token;
            public int referencedAssemblyStart;
            public int referencedAssemblyCount;
            public Il2CppAssemblyName_27_2 aname;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppAssemblyName_27_2
        {
            public IntPtr name; // const char* 
            public IntPtr culture; // const char*
            public IntPtr public_key; // const char*
            public uint hash_alg;
            public int hash_len;
            public uint flags;
            public int major;
            public int minor;
            public int build;
            public int revision;
            public fixed byte public_key_token[16]; // PUBLIC_KEY_BYTE_LENGTH
        }

        private class NativeAssemblyStruct : INativeAssemblyStruct
        {
            public NativeAssemblyStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppAssembly* AssemblyPointer => (Il2CppAssembly*)Pointer;

            private Il2CppAssembly_27_2* NativeAssembly => (Il2CppAssembly_27_2*)AssemblyPointer;

            public ref Il2CppImage* Image => ref NativeAssembly->image;

            public ref IntPtr Name => ref NativeAssembly->aname.name;

            public ref int Major => ref NativeAssembly->aname.major;

            public ref int Minor => ref NativeAssembly->aname.minor;

            public ref int Build => ref NativeAssembly->aname.build;

            public ref int Revision => ref NativeAssembly->aname.revision;
        }
    }
}
