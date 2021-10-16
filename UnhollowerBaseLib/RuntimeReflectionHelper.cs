using System;
using Il2CppSystem.Reflection;
using UnhollowerBaseLib;
using RuntimeTypeHandle = Il2CppSystem.RuntimeTypeHandle;
using Type = Il2CppSystem.Type;

namespace UnhollowerRuntimeLib
{
    public static class RuntimeReflectionHelper
    {
        public static IntPtr GetNestedTypeViaReflection(IntPtr enclosingClass, string nestedTypeName)
        {
            var reflectionType = Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(enclosingClass));
            var nestedType = reflectionType.GetNestedType(nestedTypeName, BindingFlags.Public | BindingFlags.NonPublic);
            
            return nestedType != null ? IL2CPP.il2cpp_class_from_system_type(nestedType.Pointer) : IntPtr.Zero;
        }

        public static Type GetTypeForClass(IntPtr clazz)
        {
            return Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(clazz));
        }

        public static RuntimeTypeHandle GetRuntimeTypeHandle<T>()
        {
            return Il2CppType.Of<T>().TypeHandle;
        }
    }
}