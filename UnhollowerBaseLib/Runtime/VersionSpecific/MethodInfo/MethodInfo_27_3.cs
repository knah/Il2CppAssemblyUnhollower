using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.MethodInfo
{
    [ApplicableToUnityVersionsSince("2021.2.0")]
    public unsafe class NativeMethodInfoStructHandler_27_3 : INativeMethodInfoStructHandler
    {
        public INativeMethodInfoStruct CreateNewMethodStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppMethodInfo_27_3>());
            *(Il2CppMethodInfo_27_3*)pointer = default;

            return new NativeMethodInfoStructWrapper(pointer);
        }

        public INativeMethodInfoStruct Wrap(Il2CppMethodInfo* methodPointer)
        {
            if ((IntPtr)methodPointer == IntPtr.Zero) return null;
            else return new NativeMethodInfoStructWrapper((IntPtr)methodPointer);
        }

        [DllImport("GameAssembly", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr il2cpp_method_get_from_reflection(IntPtr method);

        public IntPtr GetMethodFromReflection(IntPtr method)
        {
            return il2cpp_method_get_from_reflection(method);
        }

        public IntPtr CopyMethodInfoStruct(IntPtr origMethodInfo)
        {
            int sizeOfMethodInfo = Marshal.SizeOf<Il2CppMethodInfo_27_3>();
            IntPtr copiedMethodInfo = Marshal.AllocHGlobal(sizeOfMethodInfo);

            object temp = Marshal.PtrToStructure<Il2CppMethodInfo_27_3>(origMethodInfo);
            Marshal.StructureToPtr(temp, copiedMethodInfo, false);

            return copiedMethodInfo;
        }

        public IntPtr il2cpp_method_get_class(IntPtr method) => (IntPtr)((Il2CppMethodInfo_27_3*)method)->klass;
        public IntPtr il2cpp_method_get_name(IntPtr method) => ((Il2CppMethodInfo_27_3*)method)->name;
        public uint il2cpp_method_get_param_count(IntPtr method) => ((Il2CppMethodInfo_27_3*)method)->parameters_count;
        public IntPtr il2cpp_method_get_return_type(IntPtr method) => (IntPtr)((Il2CppMethodInfo_27_3*)method)->return_type;
        public uint il2cpp_method_get_token(IntPtr method) => ((Il2CppMethodInfo_27_3*)method)->token;

#if DEBUG
        public string GetName() => "NativeMethodInfoStructHandler_27_3";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppMethodInfo_27_3
        {
            public IntPtr methodPointer;
            public IntPtr virtualMethodPointer;
            public IntPtr invoker_method;
            public IntPtr name; // const char*
            public Il2CppClass* klass;
            public Il2CppTypeStruct* return_type;
            
            public /* Il2CppTypeStruct** */ Il2CppParameterInfo* parameters;
            // Actually a type pointer array but left as parameter info because
            // it's the same size, and it makes the wrapper code much cleaner.

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

            public MethodInfoExtraFlags_27_3 extra_flags;
        }
        
        [Flags]
        public enum MethodInfoExtraFlags_27_3 : byte
        {
            is_generic = 0x1,
            is_inflated = 0x2,
            wrapper_type = 0x4,
            has_full_generic_sharing_signature = 0x8,
            indirect_call_via_invokers = 0x10
        }


        internal class NativeMethodInfoStructWrapper : INativeMethodInfoStruct
        {
            public NativeMethodInfoStructWrapper(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public int StructSize => Marshal.SizeOf<Il2CppMethodInfo_27_3>();

            public IntPtr Pointer { get; }

            public Il2CppMethodInfo* MethodInfoPointer => (Il2CppMethodInfo*)Pointer;

            private Il2CppMethodInfo_27_3* NativeMethod => (Il2CppMethodInfo_27_3*)Pointer;

            public ref IntPtr Name => ref NativeMethod->name;

            public ref ushort Slot => ref NativeMethod->slot;

            public ref IntPtr MethodPointer => ref NativeMethod->methodPointer;

            public ref Il2CppClass* Class => ref NativeMethod->klass;

            public ref IntPtr InvokerMethod => ref NativeMethod->invoker_method;

            public ref Il2CppTypeStruct* ReturnType => ref NativeMethod->return_type;

            public ref Il2CppMethodFlags Flags => ref NativeMethod->flags;

            public ref byte ParametersCount => ref NativeMethod->parameters_count;

            public ref Il2CppParameterInfo* Parameters => ref NativeMethod->parameters;

            public bool IsGeneric
            {
                get => (NativeMethod->extra_flags & MethodInfoExtraFlags_27_3.is_generic) != 0;
                set
                {
                    if (value) NativeMethod->extra_flags |= MethodInfoExtraFlags_27_3.is_generic;
                    else NativeMethod->extra_flags &= ~MethodInfoExtraFlags_27_3.is_generic;
                }
            }

            public bool IsInflated
            {
                get => (NativeMethod->extra_flags & MethodInfoExtraFlags_27_3.is_inflated) != 0;
                set
                {
                    if (value) NativeMethod->extra_flags |= MethodInfoExtraFlags_27_3.is_inflated;
                    else NativeMethod->extra_flags &= ~MethodInfoExtraFlags_27_3.is_inflated;
                }
            }

            public bool IsMarshalledFromNative
            {
                get => false;
                set { /* no-op */ }
            }
        }
    }
}
