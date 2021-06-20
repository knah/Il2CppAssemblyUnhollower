using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.ParameterInfo
{
    [ApplicableToUnityVersionsSince("5.3.0")]
    internal class NativeParameterInfoStructHandler_16_0 : INativeParameterInfoStructHandler
    {
        public unsafe Il2CppParameterInfo*[] CreateNewParameterInfoArray(int paramCount)
        {
            var ptr = (Il2CppParameterInfo_16_0*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppParameterInfo_16_0>() * paramCount);
            var res = new Il2CppParameterInfo*[paramCount];
            for (var i = 0; i < paramCount; i++)
            {
                ptr[i] = default;
                res[i] = (Il2CppParameterInfo*) &ptr[i];
            }
            return res;
        }

        public unsafe INativeParameterInfoStruct Wrap(Il2CppParameterInfo* paramInfoPointer)
        {
            return new NativeParameterInfoStructWrapper((IntPtr) paramInfoPointer);
        }

#if DEBUG
        public string GetName() => "NativeParameterInfoStructHandler_16_0";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct Il2CppParameterInfo_16_0
        {
            public IntPtr name; // const char*
            public int position;
            public uint token;
            public int customAttributeIndex;
            public Il2CppTypeStruct* parameter_type; // const
        }

        internal unsafe class NativeParameterInfoStructWrapper : INativeParameterInfoStruct
        {
            public NativeParameterInfoStructWrapper(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppParameterInfo* ParameterInfoPointer => (Il2CppParameterInfo*)Pointer;

            public Il2CppParameterInfo_16_0* NativeParameter => (Il2CppParameterInfo_16_0*)Pointer;

            public ref IntPtr Name => ref NativeParameter->name;

            public ref int Position => ref NativeParameter->position;

            public ref uint Token => ref NativeParameter->token;

            public ref Il2CppTypeStruct* ParameterType => ref NativeParameter->parameter_type;
        }
    }
}