using System;
using System.Collections.Generic;
using System.Threading;

namespace UnhollowerBaseLib
{
    public static class RuntimeSpecificsStore
    {
        private static readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();
        private static readonly Dictionary<IntPtr, bool> UsesWeakRefsStore = new Dictionary<IntPtr, bool>();
        private static readonly Dictionary<IntPtr, bool> WasInjectedStore = new Dictionary<IntPtr, bool>();

        public static bool ShouldUseWeakRefs(IntPtr nativeClass)
        {
            Lock.EnterReadLock();
            try
            {
                return UsesWeakRefsStore.TryGetValue(nativeClass, out var result) && result;
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
        
        public static bool IsInjected(IntPtr nativeClass)
        {
            Lock.EnterReadLock();
            try
            {
                return WasInjectedStore.TryGetValue(nativeClass, out var result) && result;
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }

        public static void SetClassInfo(IntPtr nativeClass, bool useWeakRefs, bool wasInjected)
        {
            Lock.EnterWriteLock();
            try
            {
                UsesWeakRefsStore[nativeClass] = useWeakRefs;
                WasInjectedStore[nativeClass] = wasInjected;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
    }
}