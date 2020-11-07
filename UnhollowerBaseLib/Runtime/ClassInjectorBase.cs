using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime
{
    public static class ClassInjectorBase
    {
        public static object GetMonoObjectFromIl2CppPointer(IntPtr pointer)
        {
            var gcHandle = GetGcHandlePtrFromIl2CppObject(pointer);
            return GCHandle.FromIntPtr(gcHandle).Target;
        }

        public static unsafe IntPtr GetGcHandlePtrFromIl2CppObject(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero) throw new NullReferenceException();
            var objectKlass = (Il2CppClass*) IL2CPP.il2cpp_object_get_class(pointer);
            var targetGcHandlePointer = IntPtr.Add(pointer, (int) UnityVersionHandler.Wrap(objectKlass).InstanceSize - IntPtr.Size);
            var gcHandle = *(IntPtr*) targetGcHandlePointer;
            return gcHandle;
        }
    }
}