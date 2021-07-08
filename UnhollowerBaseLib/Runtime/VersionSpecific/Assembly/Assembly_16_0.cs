using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Assembly
{
    [ApplicableToUnityVersionsSince("5.3.0")]
    public unsafe class NativeAssemblyStructHandler_16_0 : INativeAssemblyStructHandler
    {
        public INativeAssemblyStruct CreateNewAssemblyStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppAssembly_16_0>());

            *(Il2CppAssembly_16_0*)pointer = default;

            return new NativeAssemblyStruct(pointer);
        }

        public INativeAssemblyStruct Wrap(Il2CppAssembly* assemblyPointer)
        {
            if ((IntPtr)assemblyPointer == IntPtr.Zero) return null;
            else return new NativeAssemblyStruct((IntPtr)assemblyPointer);
        }

        public IntPtr il2cpp_assembly_get_image(IntPtr assembly) => IL2CPP.il2cpp_assembly_get_image(assembly);
        public IntPtr il2cpp_assembly_get_name(IntPtr assembly) => ((Il2CppAssembly_16_0*)assembly)->aname.name;

#if DEBUG
        public string GetName() => "NativeAssemblyStructHandler_16_0";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppAssembly_16_0
        {
            public int imageIndex;
            public int customAttributeIndex;
            public Il2CppAssemblyName_16_0 aname;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppAssemblyName_16_0
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

            private Il2CppAssembly_16_0* NativeAssembly => (Il2CppAssembly_16_0*)Pointer;

            private Il2CppImage* dummyImagePointer;

            public ref Il2CppImage* Image => ref dummyImagePointer;

            public ref IntPtr Name => ref NativeAssembly->aname.name;

            public ref int Major => ref NativeAssembly->aname.major;

            public ref int Minor => ref NativeAssembly->aname.minor;

            public ref int Build => ref NativeAssembly->aname.build;

            public ref int Revision => ref NativeAssembly->aname.revision;
        }
    }
}