using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib
{
    public static class MethodCallScratchSpaceAllocator
    {
        [ThreadStatic]
        private static Stack<List<IntPtr>> ourEmptyLists;
        [ThreadStatic]
        private static Stack<List<IntPtr>> ourAllocated;
        
        public static void EnterMethodCall()
        {
            if (ourEmptyLists == null)
            {
                ourEmptyLists = new Stack<List<IntPtr>>();
                ourAllocated = new Stack<List<IntPtr>>();
            }
            ourAllocated.Push(ourEmptyLists.Count == 0 ? new List<IntPtr>() : ourEmptyLists.Pop());
        }

        public static void ExitMethodCall()
        {
            if (ourAllocated.Count == 0)
            {
                LogSupport.Error("Method exit triggered with empty execution stack; bug?");
                return;
            }

            var currentList = ourAllocated.Pop();
            foreach (var intPtr in currentList) 
                Marshal.FreeHGlobal(intPtr);
            
            currentList.Clear();
            
            ourEmptyLists.Push(currentList);
        }

        public static IntPtr AllocateScratchSpace(int size)
        {
            var allocated = Marshal.AllocHGlobal(size);
            if (ourAllocated.Count == 0)
            {
                LogSupport.Error("Call stack is empty; will leak memory; bug?");
                return allocated;
            }
            
            ourAllocated.Peek().Add(allocated);
            return allocated;
        }
    }
}