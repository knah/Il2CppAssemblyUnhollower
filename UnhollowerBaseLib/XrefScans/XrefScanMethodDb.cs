using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Attributes;
using UnhollowerBaseLib.Maps;

namespace UnhollowerRuntimeLib.XrefScans
{
    public static class XrefScanMethodDb
    {
        private static readonly MethodAddressToTokenMap MethodMap;
        private static readonly MethodXrefScanCache XrefScanCache;
        private static readonly long GameAssemblyBase;
        
        private static XrefScanMetadataRuntimeUtil.InitMetadataForMethod ourMetadataInitForMethodDelegate;

        static XrefScanMethodDb()
        {
            MethodMap = new MethodAddressToTokenMap(GeneratedDatabasesUtil.GetDatabasePath(MethodAddressToTokenMap.FileName));
            XrefScanCache = new MethodXrefScanCache(GeneratedDatabasesUtil.GetDatabasePath(MethodXrefScanCache.FileName));
            
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

        internal static IEnumerable<XrefInstance> ListUsers(CachedScanResultsAttribute attribute)
        {
            for (var i = attribute.RefRangeStart; i < attribute.RefRangeEnd; i++)
                yield return XrefScanCache.GetAt(i).AsXrefInstance(GameAssemblyBase);
        }

        internal static IEnumerable<XrefInstance> CachedXrefScan(CachedScanResultsAttribute attribute)
        {
            for (var i = attribute.XrefRangeStart; i < attribute.XrefRangeEnd; i++)
                yield return XrefScanCache.GetAt(i).AsXrefInstance(GameAssemblyBase);
        }
        
        internal static void CallMetadataInitForMethod(CachedScanResultsAttribute attribute)
        {
            if (attribute.MetadataInitFlagRva == 0 || attribute.MetadataInitTokenRva == 0)
                return;

            if (Marshal.ReadByte((IntPtr) (GameAssemblyBase + attribute.MetadataInitFlagRva)) != 0)
                return;

            if (ourMetadataInitForMethodDelegate == null)
                ourMetadataInitForMethodDelegate =
                    Marshal.GetDelegateForFunctionPointer<XrefScanMetadataRuntimeUtil.InitMetadataForMethod>(
                        (IntPtr) (GameAssemblyBase + XrefScanCache.Header.InitMethodMetadataRva));

            var token = Marshal.ReadInt32((IntPtr) (GameAssemblyBase + attribute.MetadataInitTokenRva));

            ourMetadataInitForMethodDelegate(token);
            
            Marshal.WriteByte((IntPtr) (GameAssemblyBase + attribute.MetadataInitFlagRva), 1);
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