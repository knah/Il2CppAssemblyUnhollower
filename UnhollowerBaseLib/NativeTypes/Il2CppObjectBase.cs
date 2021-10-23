using System;
using System.Runtime.InteropServices;
using UnhollowerBaseLib.Marshalling;
using UnhollowerBaseLib.Runtime;

namespace UnhollowerBaseLib
{
    /// <summary>
    /// todo: update casting code
    /// </summary>
    public class Il2CppObjectBase : IIl2CppObjectBase
    {
        public Il2CppObjectBase(IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
                throw new NullReferenceException();

            myGcHandle = RuntimeSpecificsStore.ShouldUseWeakRefs(IL2CPP.il2cpp_object_get_class(pointer))
                ? IL2CPP.il2cpp_gchandle_new_weakref(pointer, false)
                : IL2CPP.il2cpp_gchandle_new(pointer, false);
        }

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

        public bool WasCollected => PointerNullable == IntPtr.Zero;

        private readonly uint myGcHandle;

        public T Cast<T>() where T: Il2CppObjectBase
        {
            return TryCast<T>() ?? throw new InvalidCastException($"Can't cast object of type {Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(IL2CPP.il2cpp_object_get_class(Pointer)))} to type {typeof(T)}");
        }

        /// <summary>
        /// todo: replace with: `return this as T;` and make obsolete
        /// </summary>
        public T TryCast<T>() where T : Il2CppObjectBase
        {
            var nestedTypeClassPointer = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nestedTypeClassPointer == IntPtr.Zero)
                throw new ArgumentException($"{typeof(T)} is not an Il2Cpp reference type");

            var ownClass = IL2CPP.il2cpp_object_get_class(Pointer);
            if (!IL2CPP.il2cpp_class_is_assignable_from(nestedTypeClassPointer, ownClass))
                return null;

            if (RuntimeSpecificsStore.IsInjected(ownClass))
                return ClassInjectorBase.GetMonoObjectFromIl2CppPointer(Pointer) as T;

            return (T)Activator.CreateInstance(Il2CppClassPointerStore<T>.CreatedTypeRedirect ?? typeof(T), Pointer);
        }

        public static unsafe Il2CppObjectBase Box<T>(T valueType) where T : unmanaged
        {
            var nativeClass = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nativeClass == IntPtr.Zero) throw new ArgumentException($"Type {typeof(T)} can't be represented in Il2Cpp domain");
            return new Il2CppSystem.Object(IL2CPP.il2cpp_value_box(nativeClass, (IntPtr)(&valueType)));
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

        public static unsafe Il2CppObjectBase BoxNonBlittable<T>(T valueType) where T : IIl2CppNonBlittableValueType
        {
            var nativeClass = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nativeClass == IntPtr.Zero) throw new ArgumentException($"Type {typeof(T)} can't be represented in Il2Cpp domain");

            fixed (void* bytes = valueType.ObjectBytes)
                return new Il2CppSystem.Object(IL2CPP.il2cpp_value_box(nativeClass, (IntPtr)bytes));
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

        ~Il2CppObjectBase()
        {
            IL2CPP.il2cpp_gchandle_free(myGcHandle);
        }
    }
}