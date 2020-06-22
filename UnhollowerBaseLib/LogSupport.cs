using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib
{
    public static class LogSupport
    {
        public static event Action<string> ErrorHandler;
        public static event Action<string> WarningHandler;
        public static event Action<string> InfoHandler;
        public static event Action<string> TraceHandler;

        public static void InstallConsoleHandlers()
        {
            ErrorHandler += Console.WriteLine;
            WarningHandler += Console.WriteLine;
            InfoHandler += Console.WriteLine;
        }
        
        public static void RemoveAllHandlers()
        {
            ErrorHandler = null;
            WarningHandler = null;
            InfoHandler = null;
            TraceHandler = null;
        }

        public static void Error(string message) => ErrorHandler?.Invoke(message);
        public static void Warning(string message) => WarningHandler?.Invoke(message);
        public static void Info(string message) => InfoHandler?.Invoke(message);
        public static void Trace(string message) => TraceHandler?.Invoke(message);
    }
}