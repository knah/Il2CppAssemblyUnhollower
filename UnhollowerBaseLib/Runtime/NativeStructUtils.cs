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
    }
}