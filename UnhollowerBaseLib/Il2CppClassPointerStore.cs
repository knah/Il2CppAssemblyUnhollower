using System;
using System.Runtime.CompilerServices;

namespace UnhollowerBaseLib
{
    public static class Il2CppClassPointerStore<T>
    {
        public static IntPtr NativeClassPtr;
        
        static Il2CppClassPointerStore()
        {
            RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
        }
    }
}