using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib
{
    public static class LogSupport
    {
        public static event Action<string> LogHandler;

        static LogSupport()
        {
            LogHandler += s => OutputDebugStringA(s + "\n");
        }

        public static void Log(string message)
        {
            LogHandler?.Invoke(message);
        }

        [DllImport("kernel32", CallingConvention = CallingConvention.StdCall)]
        private static extern void OutputDebugStringA([MarshalAs(UnmanagedType.LPStr)] string chars);
    }
}