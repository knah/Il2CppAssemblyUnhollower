using System;
using UnhollowerBaseLib;

namespace UnhollowerRuntimeLib
{
    public static class Il2CppType
    {
        public static Il2CppSystem.Type TypeFromPointer(IntPtr classPointer, string typeName = "<unknown type>") => TypeFromPointerInternal(classPointer, typeName, true);

        private static Il2CppSystem.Type TypeFromPointerInternal(IntPtr classPointer, string typeName, bool throwOnFailure)
		{
            if (classPointer == IntPtr.Zero)
			{
                if (throwOnFailure) throw new ArgumentException($"{typeName} does not have a corresponding IL2CPP class pointer");
                else return null;
            }
            var il2CppType = IL2CPP.il2cpp_class_get_type(classPointer);
            if (il2CppType == IntPtr.Zero)
			{
                if (throwOnFailure) throw new ArgumentException($"{typeName} does not have a corresponding IL2CPP type pointer");
                else return null;
            }
            return Il2CppSystem.Type.internal_from_handle(il2CppType);
        }

        public static Il2CppSystem.Type From(Type type) => From(type, true);

        public static Il2CppSystem.Type From(Type type, bool throwOnFailure)
        {
            var pointer = ClassInjector.ReadClassPointerForType(type);
            return TypeFromPointerInternal(pointer, type.Name, throwOnFailure);
        }

        public static Il2CppSystem.Type Of<T>() => Of<T>(true);

        public static Il2CppSystem.Type Of<T>(bool throwOnFailure)
        {
            var classPointer = Il2CppClassPointerStore<T>.NativeClassPtr;
            return TypeFromPointerInternal(classPointer, typeof(T).Name, throwOnFailure);
        }
    }

    [Obsolete("Use Il2CppType.Of<T>()", false)]
    public static class Il2CppTypeOf<T>
    {
        [Obsolete("Use Il2CppType.Of<T>()", true)]
        public static Il2CppSystem.Type Type => Il2CppType.Of<T>();
    }
}