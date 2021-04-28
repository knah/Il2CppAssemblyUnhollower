using System;
using System.Runtime.InteropServices;
using UnhollowerBaseLib.Runtime;

namespace UnhollowerBaseLib
{
    public class Il2CppObjectBase : IIl2CppObjectBase
    {
        public Il2CppObjectBase(uint gcHandle)
        {
            myGcHandle = gcHandle;
        }

        public IntPtr Pointer
        {
            get
            {
                var handleTarget = PointerNullable;
                if (handleTarget == IntPtr.Zero) throw new ObjectCollectedException("Object was garbage collected in IL2CPP domain");
                return handleTarget;
            }
        }

        public IntPtr PointerNullable => IL2CPP.il2cpp_gchandle_get_target(myGcHandle);

        public bool IsIl2CppObjectAlive() => PointerNullable != IntPtr.Zero;

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

        [Obsolete("Use `(T) obj` instead")]
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

        public T UnboxNonBlittable<T>() where T : class, IIl2CppNonBlittableValueType
        {
            var nestedTypeClassPointer = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nestedTypeClassPointer == IntPtr.Zero)
                throw new ArgumentException($"{typeof(T)} is not an Il2Cpp reference type");

            var ownClass = IL2CPP.il2cpp_object_get_class(Pointer);
            if (!IL2CPP.il2cpp_class_is_assignable_from(nestedTypeClassPointer, ownClass))
                throw new InvalidCastException($"Can't cast object of type {Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(IL2CPP.il2cpp_object_get_class(Pointer)))} to type {typeof(T)}");

            return GenericMarshallingUtils.MarshalObjectFromPointerKnownTypeBound<T>(Pointer);
        }

        public static unsafe Il2CppObjectBase Box<T>(T valueType) where T : unmanaged
        {
            var nativeClass = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nativeClass == IntPtr.Zero) throw new ArgumentException($"Type {typeof(T)} can't be represented in Il2Cpp domain");

            return new Il2CppSystem.Object(IL2CPP.il2cpp_value_box(nativeClass, (IntPtr) (&valueType)));
        }

        public static unsafe Il2CppObjectBase BoxNonBlittable<T>(T valueType) where T : IIl2CppNonBlittableValueType
        {
            var nativeClass = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nativeClass == IntPtr.Zero) throw new ArgumentException($"Type {typeof(T)} can't be represented in Il2Cpp domain");

            fixed (void* bytes = valueType.ObjectBytes)
                return new Il2CppSystem.Object(IL2CPP.il2cpp_value_box(nativeClass, (IntPtr) bytes));
        }
        
        [Obsolete("Use `obj as T` instead")]
        public T TryCast<T>() where T: Il2CppObjectBase
        {
            return this as T;
        }

        ~Il2CppObjectBase()
        {
            IL2CPP.il2cpp_gchandle_free(myGcHandle);
        }
    }
}