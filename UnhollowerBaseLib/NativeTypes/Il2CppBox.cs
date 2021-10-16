using System;

namespace UnhollowerBaseLib
{
    public class Il2CppBox<T> : Il2CppSystem.ValueType where T : unmanaged
    {
        static Il2CppBox()
        {
            Il2CppClassPointerStore<Il2CppBox<T>>.NativeClassPtr = Il2CppClassPointerStore<T>.NativeClassPtr;
        }

        public Il2CppBox(IntPtr obj0) : base(obj0)
        {
        }

        public unsafe ref T UnboxIl2CppValue()
        {
            return ref *(T*)IL2CPP.il2cpp_object_unbox(Pointer);
        }
    }
}
