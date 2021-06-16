using System;
using System.Runtime.InteropServices;
using UnhollowerBaseLib.Runtime;

namespace UnhollowerBaseLib
{
    public class Il2CppObjectBase
    {
        public IntPtr Pointer
        {
            get
            {
                var handleTarget = IL2CPP.il2cpp_gchandle_get_target(myGcHandle);
                if (handleTarget == IntPtr.Zero) throw new ObjectCollectedException("Object was garbage collected in IL2CPP domain");
                return handleTarget;
            }
        }

        public bool WasCollected
        {
            get
            {
                var handleTarget = IL2CPP.il2cpp_gchandle_get_target(myGcHandle);
                if (handleTarget == IntPtr.Zero) return true;
                return false;
            }
        }

        private readonly uint myGcHandle;

        public Il2CppObjectBase(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
                throw new NullReferenceException();

            myGcHandle = RuntimeSpecificsStore.ShouldUseWeakRefs(IL2CPP.il2cpp_object_get_class(pointer))
                ? IL2CPP.il2cpp_gchandle_new_weakref(pointer, false)
                : IL2CPP.il2cpp_gchandle_new(pointer, false);
        }

        public T Cast<T>() where T: Il2CppObjectBase
        {
            return TryCast<T>() ?? throw new InvalidCastException($"Can't cast object of type {Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(IL2CPP.il2cpp_object_get_class(Pointer)))} to type {typeof(T)}");
        }

        public T Unbox<T>() where T : unmanaged
        {
            var nestedTypeClassPointer = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nestedTypeClassPointer == IntPtr.Zero)
                throw new ArgumentException($"{typeof(T)} is not an Il2Cpp reference type");
            
            var ownClass = IL2CPP.il2cpp_object_get_class(Pointer);
            if (!IL2CPP.il2cpp_class_is_assignable_from(nestedTypeClassPointer, ownClass))
                throw new InvalidCastException($"Can't cast object of type {Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(IL2CPP.il2cpp_object_get_class(Pointer)))} to type {typeof(T)}");

            return Marshal.PtrToStructure<T>(IL2CPP.il2cpp_object_unbox(Pointer));
        } 
        
        public T TryCast<T>() where T: Il2CppObjectBase
        {
            var nestedTypeClassPointer = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nestedTypeClassPointer == IntPtr.Zero)
                throw new ArgumentException($"{typeof(T)} is not an Il2Cpp reference type");

            var ownClass = IL2CPP.il2cpp_object_get_class(Pointer);
            if (!IL2CPP.il2cpp_class_is_assignable_from(nestedTypeClassPointer, ownClass))
                return null;

            if (RuntimeSpecificsStore.IsInjected(ownClass))
                return ClassInjectorBase.GetMonoObjectFromIl2CppPointer(Pointer) as T;

            return (T) Activator.CreateInstance(Il2CppClassPointerStore<T>.CreatedTypeRedirect ?? typeof(T), Pointer);
        }

        ~Il2CppObjectBase()
        {
            IL2CPP.il2cpp_gchandle_free(myGcHandle);
        }
    }
}