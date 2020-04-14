using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace UnhollowerBaseLib
{
    public static class Il2CppClassPointerStore<T>
    {
        public static IntPtr NativeClassPtr;
        
        static Il2CppClassPointerStore()
        {
            RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
            if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
            {
                RuntimeHelpers.RunClassConstructor(AppDomain.CurrentDomain.GetAssemblies()
                    .Single(it => it.GetName().Name == "Il2Cppmscorlib").GetType("Il2Cpp" + typeof(T).FullName)
                    .TypeHandle);
            }
        }
    }
}