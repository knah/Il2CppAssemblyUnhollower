using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.ParameterInfo
{
    [ApplicableToUnityVersionsSince("2021.2.0")]
    internal class NativeParameterInfoStructHandler_27_3 : INativeParameterInfoStructHandler
    {
        public unsafe Il2CppParameterInfo*[] CreateNewParameterInfoArray(int paramCount)
        {
            var ptr = (Il2CppParameterInfo_27_3*)Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppParameterInfo_27_3>() * paramCount);
            var res = new Il2CppParameterInfo*[paramCount];
            for (var i = 0; i < paramCount; i++)
            {
                ptr[i] = default;
                res[i] = (Il2CppParameterInfo*)&ptr[i];
            }
            return res;
        }

        public unsafe INativeParameterInfoStruct Wrap(Il2CppParameterInfo* paramInfoPointer)
        {
            if ((IntPtr)paramInfoPointer == IntPtr.Zero) return null;
            else return new NativeParameterInfoStructWrapper((IntPtr)paramInfoPointer);
        }

        public unsafe INativeParameterInfoStruct Wrap(Il2CppParameterInfo* paramInfoListBegin, int index)
        {
            if ((IntPtr)paramInfoListBegin == IntPtr.Zero) return null;
            else return new NativeParameterInfoStructWrapper((IntPtr) paramInfoListBegin + (Marshal.SizeOf<Il2CppParameterInfo_27_3>() * index));
        }

        public bool HasNamePosToken => false;

#if DEBUG
        public string GetName() => "NativeParameterInfoStructHandler_27_3";
#endif

        //Doesn't actually exist; just using this for type pointer storage in MethodInfo 27_3 +
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct Il2CppParameterInfo_27_3
        {
            public Il2CppTypeStruct* parameter_type;
        }

        internal unsafe class NativeParameterInfoStructWrapper : INativeParameterInfoStruct
        {
            public NativeParameterInfoStructWrapper(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppParameterInfo* ParameterInfoPointer => (Il2CppParameterInfo*)Pointer;

            public bool HasNamePosToken => false;

            private Il2CppParameterInfo_27_3* NativeParameter => (Il2CppParameterInfo_27_3*)Pointer;

            public ref IntPtr Name => throw new NotSupportedException("ParameterInfo does not exist in Unity 2021.2.0+");

            public ref int Position => throw new NotSupportedException("ParameterInfo does not exist in Unity 2021.2.0+");

            public ref uint Token => throw new NotSupportedException("ParameterInfo does not exist in Unity 2021.2.0+");

            public ref Il2CppTypeStruct* ParameterType => ref NativeParameter->parameter_type;
        }
    }
}
