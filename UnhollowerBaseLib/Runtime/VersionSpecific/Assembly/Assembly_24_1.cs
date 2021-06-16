using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Assembly
{
    [ApplicableToUnityVersionsSince("2018.3.0")]
    [ApplicableToUnityVersionsSince("2020.1.0")]
    public unsafe class NativeAssemblyStructHandler_24_1 : INativeAssemblyStructHandler
    {
        public INativeAssemblyStruct CreateNewAssemblyStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppAssembly_24_1>());

            *(Il2CppAssembly_24_1*)pointer = default;

            return new NativeAssemblyStruct(pointer);
        }

        public INativeAssemblyStruct Wrap(Il2CppAssembly* assemblyPointer)
        {
            return new NativeAssemblyStruct((IntPtr)assemblyPointer);
        }

        public string GetName() => "NativeAssemblyStructHandler_24_1";

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppAssembly_24_1
        {
            public Il2CppImage* image;
            public uint token;
            public int referencedAssemblyStart;
            public int referencedAssemblyCount;
            public Il2CppAssemblyName_24_1 aname;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppAssemblyName_24_1
        {
            public IntPtr name; // const char* 
            public IntPtr culture; // const char*
            public IntPtr hash_value; // const char*
            public IntPtr public_key; // const char*
            public uint hash_alg;
            public int hash_len;
            public uint flags;
            public int major;
            public int minor;
            public int build;
            public int revision;
            public long public_key_token; // PUBLIC_KEY_BYTE_LENGTH
        }

        internal class NativeAssemblyStruct : INativeAssemblyStruct
        {
            public NativeAssemblyStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppAssembly* AssemblyPointer => (Il2CppAssembly*)Pointer;

            private Il2CppAssembly_24_1* NativeAssembly => (Il2CppAssembly_24_1*)Pointer;

            public ref Il2CppImage* Image => ref NativeAssembly->image;

            public ref IntPtr Name => ref NativeAssembly->aname.name;

            public ref int Major => ref NativeAssembly->aname.major;

            public ref int Minor => ref NativeAssembly->aname.minor;

            public ref int Build => ref NativeAssembly->aname.build;

            public ref int Revision => ref NativeAssembly->aname.revision;
        }
    }
}
