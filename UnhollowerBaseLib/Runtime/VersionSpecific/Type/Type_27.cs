using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Type
{
    [ApplicableToUnityVersionsSince("2020.2.0")]
    public unsafe class NativeTypeStructHandler_27 : INativeTypeStructHandler
    {
        public INativeTypeStruct CreateNewTypeStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppType_27>());

            *(Il2CppType_27*)pointer = default;

            return new NativeTypeStruct(pointer);
        }

        public INativeTypeStruct Wrap(Il2CppTypeStruct* TypePointer)
        {
            return new NativeTypeStruct((IntPtr)TypePointer);
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppType_27
        {
            /*union
            {
                // We have this dummy field first because pre C99 compilers (MSVC) can only initializer the first value in a union.
                void* dummy;
                TypeDefinitionIndex klassIndex; /* for VALUETYPE and CLASS #1#
                Il2CppMetadataTypeHandle typeHandle;
                const Il2CppType *type;   /* for PTR and SZARRAY #1#
                Il2CppArrayType *array; /* for ARRAY #1#
                //MonoMethodSignature *method;
                GenericParameterIndex genericParameterIndex; /* for VAR and MVAR #1#
                Il2CppMetadataGenericParameterHandle genericParameterHandle;
                Il2CppGenericClass *generic_class; /* for GENERICINST #1#
            } data;*/
            public IntPtr data;

            public ushort attrs;
            public Il2CppTypeEnum type;
            public byte mods_byref_pin;
            /*unsigned int attrs    : 16; /* param attributes or field flags #1#
            Il2CppTypeEnum type     : 8;
            unsigned int num_mods : 6;  /* max 64 modifiers follow at the end #1#
            unsigned int byref    : 1;
            unsigned int pinned   : 1;  /* valid when included in a local var signature #1#*/
            //MonoCustomMod modifiers [MONO_ZERO_LEN_ARRAY]; /* this may grow */
        }

        private class NativeTypeStruct : INativeTypeStruct
        {
            public NativeTypeStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppTypeStruct* TypePointer => (Il2CppTypeStruct*)Pointer;

            private Il2CppType_27* NativeType => (Il2CppType_27*)TypePointer;

            public ref Il2CppTypeEnum Type => ref NativeType->type;

            public ref IntPtr Data => ref NativeType->data;
        }
    }
}
