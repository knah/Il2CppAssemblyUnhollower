using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime
{
    public static class NativeStructUtils
    {
        public static unsafe IntPtr GetMethodInfoForMissingMethod(string methodName)
        {
            var methodInfo = (Il2CppMethodInfo*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppMethodInfo>());
            *methodInfo = default;
            methodInfo->name = Marshal.StringToHGlobalAnsi(methodName);
            methodInfo->slot = UInt16.MaxValue;

            return (IntPtr) methodInfo;
        }

        public static unsafe bool CheckBit(this INativeStruct self, int startOffset, int bit)
        {
            var byteOffset = bit / 8;
            var bitOffset = bit % 8;
            var p = self.Pointer + startOffset + byteOffset;
            
            var mask = 1 << bitOffset;
            var val = *(byte*) p.ToPointer();
            var masked = val & mask;
            return masked == mask;
        }

        public static unsafe void SetBit(this INativeStruct self, int startOffset, int bit, bool value)
        {
            var byteOffset = bit / 8;
            var bitOffset = bit % 8;
            var p = self.Pointer + startOffset + byteOffset;

            var mask = ~(1 << bitOffset);
            var ptr = (byte*) p.ToPointer();
            var val = *ptr;
            var newVal = (byte)(val & mask | ((value ? 1 : 0) << bitOffset));
            *ptr = newVal;
        }
    }
}