using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnhollowerBaseLib;

namespace UnhollowerRuntimeLib.XrefScans
{
    public static class XrefScanMethodDb
    {
        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
        private static readonly HashSet<Type> RegisteredTypes = new HashSet<Type>();
        private static readonly Dictionary<IntPtr, MethodBase> MethodMap = new Dictionary<IntPtr, MethodBase>();
        
        public static MethodBase TryResolvePointer(IntPtr methodStart)
        {
            try
            {
                Lock.EnterReadLock();
                MethodMap.TryGetValue(methodStart, out var result);
                return result;
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }

        public static unsafe void RegisterType(Type type)
        {
            try
            {
                Lock.EnterUpgradeableReadLock();
                if (RegisteredTypes.Contains(type)) return;

                try
                {
                    Lock.EnterWriteLock();
                    RegisteredTypes.Add(type);

                    void ProcessMethod(MethodBase method)
                    {
                        if (method.GetMethodBody() == null) return;
                        var pointerField = UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method);
                        if (pointerField == null) return;
                        MethodMap[*(IntPtr*) (IntPtr) pointerField.GetValue(null)] = method;
                    }

                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    foreach (var methodInfo in methods) 
                        ProcessMethod(methodInfo);
                    
                    var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    foreach (var methodInfo in ctors) 
                        ProcessMethod(methodInfo);
                }
                finally
                {
                    Lock.ExitWriteLock();
                }
            }
            finally
            {
                Lock.ExitUpgradeableReadLock();
            }
            if (type.BaseType != null)
                RegisterType(type.BaseType);
        }

        public static void RegisterType<T>() => RegisterType(typeof(T));
    }
}