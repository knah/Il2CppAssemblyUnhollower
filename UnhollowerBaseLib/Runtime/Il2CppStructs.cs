using System;
using System.Runtime.InteropServices;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace UnhollowerBaseLib.Runtime
{
    [Flags]
    public enum Il2CppMethodImplFlags : ushort
    {
        METHOD_IMPL_ATTRIBUTE_CODE_TYPE_MASK = 0x0003,
        METHOD_IMPL_ATTRIBUTE_IL = 0x0000,
        METHOD_IMPL_ATTRIBUTE_NATIVE = 0x0001,
        METHOD_IMPL_ATTRIBUTE_OPTIL = 0x0002,
        METHOD_IMPL_ATTRIBUTE_RUNTIME = 0x0003,

        METHOD_IMPL_ATTRIBUTE_MANAGED_MASK = 0x0004,
        METHOD_IMPL_ATTRIBUTE_UNMANAGED = 0x0004,
        METHOD_IMPL_ATTRIBUTE_MANAGED = 0x0000,

        METHOD_IMPL_ATTRIBUTE_FORWARD_REF = 0x0010,
        METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG = 0x0080,
        METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL = 0x1000,
        METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED = 0x0020,
        METHOD_IMPL_ATTRIBUTE_NOINLINING = 0x0008,
        METHOD_IMPL_ATTRIBUTE_MAX_METHOD_IMPL_VAL = 0xffff,
    }

    [Flags]
    public enum Il2CppMethodFlags : ushort
    {
        METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK = 0x0007,
        METHOD_ATTRIBUTE_COMPILER_CONTROLLED = 0x0000,
        METHOD_ATTRIBUTE_PRIVATE = 0x0001,
        METHOD_ATTRIBUTE_FAM_AND_ASSEM = 0x0002,
        METHOD_ATTRIBUTE_ASSEM = 0x0003,
        METHOD_ATTRIBUTE_FAMILY = 0x0004,
        METHOD_ATTRIBUTE_FAM_OR_ASSEM = 0x0005,
        METHOD_ATTRIBUTE_PUBLIC = 0x0006,

        METHOD_ATTRIBUTE_STATIC = 0x0010,
        METHOD_ATTRIBUTE_FINAL = 0x0020,
        METHOD_ATTRIBUTE_VIRTUAL = 0x0040,
        METHOD_ATTRIBUTE_HIDE_BY_SIG = 0x0080,

        METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK = 0x0100,
        METHOD_ATTRIBUTE_REUSE_SLOT = 0x0000,
        METHOD_ATTRIBUTE_NEW_SLOT = 0x0100,

        METHOD_ATTRIBUTE_STRICT = 0x0200,
        METHOD_ATTRIBUTE_ABSTRACT = 0x0400,
        METHOD_ATTRIBUTE_SPECIAL_NAME = 0x0800,

        METHOD_ATTRIBUTE_PINVOKE_IMPL = 0x2000,
        METHOD_ATTRIBUTE_UNMANAGED_EXPORT = 0x0008,
        
        /*
         * For runtime use only
         */
        METHOD_ATTRIBUTE_RESERVED_MASK = 0xd000,
        METHOD_ATTRIBUTE_RT_SPECIAL_NAME = 0x1000,
        METHOD_ATTRIBUTE_HAS_SECURITY = 0x4000,
        METHOD_ATTRIBUTE_REQUIRE_SEC_OBJECT = 0x8000,
    }

    [Flags]
    public enum Il2CppClassAttributes : uint
    {
        TYPE_ATTRIBUTE_VISIBILITY_MASK = 0x00000007,
        TYPE_ATTRIBUTE_NOT_PUBLIC = 0x00000000,
        TYPE_ATTRIBUTE_PUBLIC = 0x00000001,
        TYPE_ATTRIBUTE_NESTED_PUBLIC = 0x00000002,
        TYPE_ATTRIBUTE_NESTED_PRIVATE = 0x00000003,
        TYPE_ATTRIBUTE_NESTED_FAMILY = 0x00000004,
        TYPE_ATTRIBUTE_NESTED_ASSEMBLY = 0x00000005,
        TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM = 0x00000006,
        TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM = 0x00000007,

        TYPE_ATTRIBUTE_LAYOUT_MASK = 0x00000018,
        TYPE_ATTRIBUTE_AUTO_LAYOUT = 0x00000000,
        TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT = 0x00000008,
        TYPE_ATTRIBUTE_EXPLICIT_LAYOUT = 0x00000010,

        TYPE_ATTRIBUTE_CLASS_SEMANTIC_MASK = 0x00000020,
        TYPE_ATTRIBUTE_CLASS = 0x00000000,
        TYPE_ATTRIBUTE_INTERFACE = 0x00000020,

        TYPE_ATTRIBUTE_ABSTRACT = 0x00000080,
        TYPE_ATTRIBUTE_SEALED = 0x00000100,
        TYPE_ATTRIBUTE_SPECIAL_NAME = 0x00000400,

        TYPE_ATTRIBUTE_IMPORT = 0x00001000,
        TYPE_ATTRIBUTE_SERIALIZABLE = 0x00002000,

        TYPE_ATTRIBUTE_STRING_FORMAT_MASK = 0x00030000,
        TYPE_ATTRIBUTE_ANSI_CLASS = 0x00000000,
        TYPE_ATTRIBUTE_UNICODE_CLASS = 0x00010000,
        TYPE_ATTRIBUTE_AUTO_CLASS = 0x00020000,

        TYPE_ATTRIBUTE_BEFORE_FIELD_INIT = 0x00100000,
        TYPE_ATTRIBUTE_FORWARDER = 0x00200000,

        TYPE_ATTRIBUTE_RESERVED_MASK = 0x00040800,
        TYPE_ATTRIBUTE_RT_SPECIAL_NAME = 0x00000800,
        TYPE_ATTRIBUTE_HAS_SECURITY = 0x00040000,
    }

    public enum Il2CppTypeEnum : byte
    {
        IL2CPP_TYPE_END        = 0x00,       /* End of List */
        IL2CPP_TYPE_VOID       = 0x01,
        IL2CPP_TYPE_BOOLEAN    = 0x02,
        IL2CPP_TYPE_CHAR       = 0x03,
        IL2CPP_TYPE_I1         = 0x04,
        IL2CPP_TYPE_U1         = 0x05,
        IL2CPP_TYPE_I2         = 0x06,
        IL2CPP_TYPE_U2         = 0x07,
        IL2CPP_TYPE_I4         = 0x08,
        IL2CPP_TYPE_U4         = 0x09,
        IL2CPP_TYPE_I8         = 0x0a,
        IL2CPP_TYPE_U8         = 0x0b,
        IL2CPP_TYPE_R4         = 0x0c,
        IL2CPP_TYPE_R8         = 0x0d,
        IL2CPP_TYPE_STRING     = 0x0e,
        IL2CPP_TYPE_PTR        = 0x0f,       /* arg: <type> token */
        IL2CPP_TYPE_BYREF      = 0x10,       /* arg: <type> token */
        IL2CPP_TYPE_VALUETYPE  = 0x11,       /* arg: <type> token */
        IL2CPP_TYPE_CLASS      = 0x12,       /* arg: <type> token */
        IL2CPP_TYPE_VAR        = 0x13,       /* Generic parameter in a generic type definition, represented as number (compressed unsigned integer) number */
        IL2CPP_TYPE_ARRAY      = 0x14,       /* type, rank, boundsCount, bound1, loCount, lo1 */
        IL2CPP_TYPE_GENERICINST = 0x15,     /* <type> <type-arg-count> <type-1> \x{2026} <type-n> */
        IL2CPP_TYPE_TYPEDBYREF = 0x16,
        IL2CPP_TYPE_I          = 0x18,
        IL2CPP_TYPE_U          = 0x19,
        IL2CPP_TYPE_FNPTR      = 0x1b,        /* arg: full method signature */
        IL2CPP_TYPE_OBJECT     = 0x1c,
        IL2CPP_TYPE_SZARRAY    = 0x1d,       /* 0-based one-dim-array */
        IL2CPP_TYPE_MVAR       = 0x1e,       /* Generic parameter in a generic method definition, represented as number (compressed unsigned integer)  */
        IL2CPP_TYPE_CMOD_REQD  = 0x1f,       /* arg: typedef or typeref token */
        IL2CPP_TYPE_CMOD_OPT   = 0x20,       /* optional arg: typedef or typref token */
        IL2CPP_TYPE_INTERNAL   = 0x21,       /* CLR internal type */

        IL2CPP_TYPE_MODIFIER   = 0x40,       /* Or with the following types */
        IL2CPP_TYPE_SENTINEL   = 0x41,       /* Sentinel for varargs method signature */
        IL2CPP_TYPE_PINNED     = 0x45,       /* Local var that points to pinned object */

        IL2CPP_TYPE_ENUM       = 0x55        /* an enumeration */
    }
    
    public struct Il2CppMethodInfo
    {
    }

    [Flags]
    public enum MethodInfoExtraFlags : byte
    {
        is_generic = 0x1,
        is_inflated = 0x2,
        wrapper_type = 0x4,
        is_marshalled_from_native = 0x8
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct VirtualInvokeData
    {
        public IntPtr methodPtr;
        public Il2CppMethodInfo* method;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct FieldInfo
    {
        public IntPtr name; // const char*
        public Il2CppTypeStruct* type; // const
        public Il2CppClass* parent; // non-const?
        public int offset; // If offset is -1, then it's thread static
        public uint token;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct PropertyInfo
    {
        public Il2CppClass* parent;
        public IntPtr name; // const char*
        public Il2CppMethodInfo *get; // const
        public Il2CppMethodInfo *set; // const
        public uint attrs;
        public uint token;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct EventInfo
    {
        public IntPtr name; // const char*
        public Il2CppTypeStruct* eventType; // const
        public Il2CppClass* parent; // non const
        public Il2CppMethodInfo* add; // const
        public Il2CppMethodInfo* remove; // const
        public Il2CppMethodInfo* raise; // const
        public uint token;
    }

    public struct Il2CppParameterInfo
    {
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Il2CppTypeStruct
    {
        /*union
        {
            // We have this dummy field first because pre C99 compilers (MSVC) can only initializer the first value in a union.
            void* dummy;
            TypeDefinitionIndex klassIndex; /* for VALUETYPE and CLASS #1#
            const Il2CppType *type;   /* for PTR and SZARRAY #1#
            Il2CppArrayType *array; /* for ARRAY #1#
            //MonoMethodSignature *method;
            GenericParameterIndex genericParameterIndex; /* for VAR and MVAR #1#
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
    
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Il2CppRuntimeInterfaceOffsetPair
    {
        public Il2CppClass* interfaceType;
        public int offset;
    }

    [Flags]
    public enum ClassBitfield2 : byte
    {
        has_finalize = 0x1,
        has_cctor = 0x2,
        is_blittable = 0x4,
        is_import_or_windows_runtime = 0x8,
        is_vtable_initialized = 0x10,
        has_initialization_error = 0x20
    }

    [Flags]
    public enum ClassBitfield1 : byte
    {
        initialized_and_no_error = 0x1,
        valuetype = 0x2,
        initialized = 0x4,
        enumtype = 0x8,
        is_generic = 0x10,
        has_references = 0x20,
        init_pending = 0x40,
        size_inited = 0x80
    }
    
    public struct Il2CppImage
    {
    }

    public struct Il2CppAssembly
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Il2CppClass
    {
    } // stub struct

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Il2CppClassPart1
    {
        // The following fields are always valid for a Il2CppClass structure
        public Il2CppImage* image; // const
        public IntPtr gc_desc;
        public IntPtr name; // const char*
        public IntPtr namespaze; // const char*
        public Il2CppTypeStruct byval_arg; // not const, no ptr
        public Il2CppTypeStruct this_arg; // not const, no ptr
        public Il2CppClass* element_class; // not const
        public Il2CppClass* castClass; // not const
        public Il2CppClass* declaringType; // not const
        public Il2CppClass* parent; // not const
        public /*Il2CppGenericClass**/IntPtr generic_class;
        public /*Il2CppTypeDefinition**/IntPtr typeDefinition; // const; non-NULL for Il2CppClass's constructed from type defintions
        public /*Il2CppInteropData**/IntPtr interopData; // const
        public Il2CppClass* klass; // not const; hack to pretend we are a MonoVTable. Points to ourself
        // End always valid fields

        // The following fields need initialized before access. This can be done per field or as an aggregate via a call to Class::Init
        public FieldInfo* fields; // Initialized in SetupFields
        public EventInfo* events; // const; Initialized in SetupEvents
        public PropertyInfo* properties; // const; Initialized in SetupProperties
        public Il2CppMethodInfo** methods; // const; Initialized in SetupMethods
        public Il2CppClass** nestedTypes; // not const; Initialized in SetupNestedTypes
        public Il2CppClass** implementedInterfaces; // not const; Initialized in SetupInterfaces
        public Il2CppRuntimeInterfaceOffsetPair* interfaceOffsets; // not const; Initialized in Init
        public IntPtr static_fields; // not const; Initialized in Init
        public /*Il2CppRGCTXData**/IntPtr rgctx_data; // const; Initialized in Init
        // used for fast parent checks
        public Il2CppClass** typeHierarchy; // not const; Initialized in SetupTypeHierachy
        // End initialization required fields
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Il2CppClassPart2
    {
        public uint initializationExceptionGCHandle;

        public uint cctor_started;
        public uint cctor_finished;
        /*ALIGN_TYPE(8)*/ulong cctor_thread;

        // Remaining fields are always valid except where noted
        public /*GenericContainerIndex*/ int genericContainerIndex;
        public uint instance_size;
        public uint actualSize;
        public uint element_size;
        public int native_size;
        public uint static_fields_size;
        public uint thread_static_fields_size;
        public int thread_static_fields_offset;
        public Il2CppClassAttributes flags;
        public uint token;

        public ushort method_count; // lazily calculated for arrays, i.e. when rank > 0
        public ushort property_count;
        public ushort field_count;
        public ushort event_count;
        public ushort nested_type_count;
        public ushort vtable_count; // lazily calculated for arrays, i.e. when rank > 0
        public ushort interfaces_count;
        public ushort interface_offsets_count; // lazily calculated for arrays, i.e. when rank > 0

        public byte typeHierarchyDepth; // Initialized in SetupTypeHierachy
        public byte genericRecursionDepth;
        public byte rank;
        public byte minimumAlignment; // Alignment of this type
        public byte naturalAligment; // Alignment of this type without accounting for packing
        public byte packingSize;

        // this is critical for performance of Class::InitFromCodegen. Equals to initialized && !has_initialization_error at all times.
        // Use Class::UpdateInitializedAndNoError to update
        public ClassBitfield1 bitfield_1;
        /*uint8_t initialized_and_no_error : 1;

        uint8_t valuetype : 1;
        uint8_t initialized : 1;
        uint8_t enumtype : 1;
        uint8_t is_generic : 1;
        uint8_t has_references : 1;
        uint8_t init_pending : 1;
        uint8_t size_inited : 1;*/

        public ClassBitfield2 bitfield_2;
        /*uint8_t has_finalize : 1;
        uint8_t has_cctor : 1;
        uint8_t is_blittable : 1;
        uint8_t is_import_or_windows_runtime : 1;
        uint8_t is_vtable_initialized : 1;
        uint8_t has_initialization_error : 1;*/

        //VirtualInvokeData vtable[IL2CPP_ZERO_LEN_ARRAY];
    }
}