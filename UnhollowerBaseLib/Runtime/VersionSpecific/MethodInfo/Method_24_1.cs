using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.MethodInfo
{
    [ApplicableToUnityVersionsSince("2018.3.0")]
    public unsafe class NativeMethodStructHandler_24_1 : INativeMethodStructHandler
    {
        public INativeMethodStruct CreateNewMethodStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppMethodInfoU2018>());
            *(Il2CppMethodInfoU2018*)pointer = default;

            return new NativeMethodInfoStructWrapper(pointer);
        }

        public INativeMethodStruct Wrap(Il2CppMethodInfo* methodPointer)
        {
            return new NativeMethodInfoStructWrapper((IntPtr)methodPointer);
        }

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr il2cpp_method_get_from_reflection(IntPtr method);

        public IntPtr GetMethodFromReflection(IntPtr method)
        {
            return il2cpp_method_get_from_reflection(method);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Il2CppMethodInfoU2018
        {
            public IntPtr methodPointer;
            public IntPtr invoker_method;
            public IntPtr name; // const char*
            public Il2CppClass* klass;
            public Il2CppTypeStruct* return_type;
            public Il2CppParameterInfo* parameters;

            public IntPtr someRtData;
            /*union
            {
                const Il2CppRGCTXData* rgctx_data; /* is_inflated is true and is_generic is false, i.e. a generic instance method #1#
                const Il2CppMethodDefinition* methodDefinition;
            };*/

            public IntPtr someGenericData;
            /*/* note, when is_generic == true and is_inflated == true the method represents an uninflated generic method on an inflated type. #1#
            union
            {
                const Il2CppGenericMethod* genericMethod; /* is_inflated is true #1#
                const Il2CppGenericContainer* genericContainer; /* is_inflated is false and is_generic is true #1#
            };*/

            public uint token;
            public Il2CppMethodFlags flags;
            public Il2CppMethodImplFlags iflags;
            public ushort slot;
            public byte parameters_count;

            public MethodInfoExtraFlags extra_flags;
            /*uint8_t is_generic : 1; /* true if method is a generic method definition #1#
            uint8_t is_inflated : 1; /* true if declaring_type is a generic instance or if method is a generic instance#1#
            uint8_t wrapper_type : 1; /* always zero (MONO_WRAPPER_NONE) needed for the debugger #1#
            uint8_t is_marshaled_from_native : 1*/
        }


        private class NativeMethodInfoStructWrapper : INativeMethodStruct
        {
            public NativeMethodInfoStructWrapper(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public int StructSize => Marshal.SizeOf<Il2CppMethodInfoU2018>();

            public IntPtr Pointer { get; }

            public Il2CppMethodInfo* MethodInfoPointer => (Il2CppMethodInfo*)Pointer;

            private Il2CppMethodInfoU2018* NativeMethod => (Il2CppMethodInfoU2018*)Pointer;

            public ref IntPtr Name => ref NativeMethod->name;

            public ref ushort Slot => ref NativeMethod->slot;

            public ref IntPtr MethodPointer => ref NativeMethod->methodPointer;

            public ref Il2CppClass* Class => ref NativeMethod->klass;

            public ref IntPtr InvokerMethod => ref NativeMethod->invoker_method;

            public ref Il2CppTypeStruct* ReturnType => ref NativeMethod->return_type;

            public ref Il2CppMethodFlags Flags => ref NativeMethod->flags;

            public ref byte ParametersCount => ref NativeMethod->parameters_count;

            public ref Il2CppParameterInfo* Parameters => ref NativeMethod->parameters;

            public ref MethodInfoExtraFlags ExtraFlags => ref NativeMethod->extra_flags;
        }
    }
}