using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Assembly
{
	[ApplicableToUnityVersionsSince("2017.1.0")]
	public unsafe class NativeAssemblyStructHandler_24_A : INativeAssemblyStructHandler
	{
		public INativeAssemblyStruct CreateNewAssemblyStruct()
		{
			var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppAssembly_24_A>());

			*(Il2CppAssembly_24_A*)pointer = default;

			return new NativeAssemblyStruct(pointer);
		}

		public INativeAssemblyStruct Wrap(Il2CppAssembly* assemblyPointer)
		{
			return new NativeAssemblyStruct((IntPtr)assemblyPointer);
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Il2CppAssembly_24_A
		{
			public Il2CppImage* image;
			public IntPtr customAttribute;
			public int referencedAssemblyStart;
			public int referencedAssemblyCount;
			public Il2CppAssemblyName_24_A aname;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Il2CppAssemblyName_24_A
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

			private Il2CppAssembly_24_A* NativeAssembly => (Il2CppAssembly_24_A*)AssemblyPointer;

			public ref Il2CppImage* Image => ref NativeAssembly->image;

			public ref IntPtr Name => ref NativeAssembly->aname.name;
		}
	}
}
