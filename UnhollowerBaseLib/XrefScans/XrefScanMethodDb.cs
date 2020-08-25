using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnhollowerBaseLib.Maps;

namespace UnhollowerRuntimeLib.XrefScans
{
    public static class XrefScanMethodDb
    {
        private static readonly MethodAddressToTokenMap MethodMap;
        private static readonly long GameAssemblyBase;

        static XrefScanMethodDb()
        {
            var databasePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, MethodAddressToTokenMap.FileName);
            MethodMap = new MethodAddressToTokenMap(databasePath);
            
            foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
            {
                if (module.ModuleName == "GameAssembly.dll")
                {
                    GameAssemblyBase = (long) module.BaseAddress;
                    break;
                }
            }
        }

        public static MethodBase TryResolvePointer(IntPtr methodStart)
        {
            return MethodMap.Lookup((long) methodStart - GameAssemblyBase);
        }

        [Obsolete("Type registration is no longer needed")]
        public static void RegisterType(Type type)
        {
        }

        [Obsolete("Type registration is no longer needed")]
        public static void RegisterType<T>()
        {
        }
    }
}