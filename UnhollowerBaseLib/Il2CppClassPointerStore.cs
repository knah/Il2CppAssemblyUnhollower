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
            var targetType = typeof(T);
            RuntimeHelpers.RunClassConstructor(targetType.TypeHandle);
            if (targetType.IsPrimitive || targetType == typeof(string))
            {
                RuntimeHelpers.RunClassConstructor(AppDomain.CurrentDomain.GetAssemblies()
                    .Single(it => it.GetName().Name == "Il2Cppmscorlib").GetType("Il2Cpp" + targetType.FullName)
                    .TypeHandle);
            }
            
            foreach (var customAttribute in targetType.CustomAttributes)
            {
                if (customAttribute.AttributeType != typeof(AlsoInitializeAttribute)) continue;
                
                var linkedType = (Type) customAttribute.ConstructorArguments[0].Value;
                RuntimeHelpers.RunClassConstructor(linkedType.TypeHandle);
            }
        }
    }
}