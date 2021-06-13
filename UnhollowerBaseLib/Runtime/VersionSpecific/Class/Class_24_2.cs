using System;
using System.Runtime.InteropServices;
using UnhollowerBaseLib.Runtime.VersionSpecific.Type;
using TypeStruct = UnhollowerBaseLib.Runtime.VersionSpecific.Type.NativeTypeStructHandler_24_0.Il2CppType_24_0;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Class
{
    [ApplicableToUnityVersionsSince("2019.1.0")]
    public class NativeClassStructHandler_24_2 : INativeClassStructHandler
    {
        public unsafe INativeClassStruct CreateNewClassStruct(int vTableSlots)
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppClass_24_2>() +
                                               Marshal.SizeOf<VirtualInvokeData>() * vTableSlots);

            *(Il2CppClass_24_2*)pointer = default;

            return new NativeClassStruct(pointer);
        }

        public unsafe INativeClassStruct Wrap(Il2CppClass* classPointer)
        {
            return new NativeClassStruct((IntPtr)classPointer);
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct Il2CppClass_24_2
        {
            // The following fields are always valid for a Il2CppClass structure
            public Il2CppImage* image; // const
            public IntPtr gc_desc;
            public IntPtr name; // const char*
            public IntPtr namespaze; // const char*
            public TypeStruct byval_arg; // not const, no ptr
            public TypeStruct this_arg; // not const, no ptr
            public Il2CppClass* element_class; // not const
            public Il2CppClass* castClass; // not const
            public Il2CppClass* declaringType; // not const
            public Il2CppClass* parent; // not const
            public /*Il2CppGenericClass**/ IntPtr generic_class;

            public /*Il2CppTypeDefinition**/
                IntPtr typeDefinition; // const; non-NULL for Il2CppClass's constructed from type defintions

            public /*Il2CppInteropData**/ IntPtr interopData; // const

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

            public /*Il2CppRGCTXData**/ IntPtr rgctx_data; // const; Initialized in Init

            // used for fast parent checks
            public Il2CppClass** typeHierarchy; // not const; Initialized in SetupTypeHierachy
            // End initialization required fields

            // U2019 specific field
            public IntPtr unity_user_data;

            public uint initializationExceptionGCHandle;

            public uint cctor_started;

            public uint cctor_finished;

            /*ALIGN_TYPE(8)*/
            private IntPtr cctor_thread; // was uint64 in 2018.4, is size_t in 2019.3.1

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
            public byte bitfield_1;
            /*uint8_t initialized_and_no_error : 1;
    
            uint8_t valuetype : 1;
            uint8_t initialized : 1;
            uint8_t enumtype : 1;
            uint8_t is_generic : 1;
            uint8_t has_references : 1;
            uint8_t init_pending : 1;
            uint8_t size_inited : 1;*/

            public byte bitfield_2;
            /*uint8_t has_finalize : 1;
            uint8_t has_cctor : 1;
            uint8_t is_blittable : 1;
            uint8_t is_import_or_windows_runtime : 1;
            uint8_t is_vtable_initialized : 1;
            uint8_t has_initialization_error : 1;*/

            //VirtualInvokeData vtable[IL2CPP_ZERO_LEN_ARRAY];
        }

        private unsafe class NativeClassStruct : INativeClassStruct
        {
            public NativeClassStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }
            public Il2CppClass* ClassPointer => (Il2CppClass*)Pointer;

            public IntPtr VTable => IntPtr.Add(Pointer, Marshal.SizeOf<Il2CppClass_24_2>());

            private Il2CppClass_24_2* NativeClass => (Il2CppClass_24_2*)ClassPointer;

            public ref uint InstanceSize => ref NativeClass->instance_size;

            public ref ushort VtableCount => ref NativeClass->vtable_count;

            public ref int NativeSize => ref NativeClass->native_size;

            public ref uint ActualSize => ref NativeClass->actualSize;

            public ref ushort MethodCount => ref NativeClass->method_count;

            private static int bitfield1offset =
                Marshal.OffsetOf<Il2CppClass_24_2>(nameof(Il2CppClass_24_2.bitfield_1)).ToInt32();

            private static int bitfield2offset =
                Marshal.OffsetOf<Il2CppClass_24_2>(nameof(Il2CppClass_24_2.bitfield_2)).ToInt32();

            public bool ValueType
            {
                get => this.CheckBit(bitfield1offset, 1);
                set => this.SetBit(bitfield1offset, 1, value);
            }

            public bool EnumType
            {
                get => this.CheckBit(bitfield1offset, 3);
                set => this.SetBit(bitfield1offset, 3, value);
            }

            public bool IsGeneric
            {
                get => this.CheckBit(bitfield1offset, 4);
                set => this.SetBit(bitfield1offset, 4, value);
            }

            public bool Initialized
            {
                get => this.CheckBit(bitfield1offset, 2);
                set => this.SetBit(bitfield1offset, 2, value);
            }

            public bool InitializedAndNoError
            {
                get => this.CheckBit(bitfield1offset, 0);
                set => this.SetBit(bitfield1offset, 0, value);
            }

            public bool SizeInited
            {
                get => this.CheckBit(bitfield1offset, 7);
                set => this.SetBit(bitfield1offset, 7, value);
            }

            public bool HasFinalize
            {
                get => this.CheckBit(bitfield2offset, 0);
                set => this.SetBit(bitfield2offset, 0, value);
            }

            public bool IsVtableInitialized
            {
                get => this.CheckBit(bitfield2offset, 4);
                set => this.SetBit(bitfield2offset, 4, value);
            }

            public ref Il2CppClassAttributes Flags => ref NativeClass->flags;

            public ref IntPtr Name => ref NativeClass->name;

            public ref IntPtr Namespace => ref NativeClass->namespaze;

            private Il2CppTypeStruct* ByValArgPointer => (Il2CppTypeStruct*)(&(NativeClass->byval_arg));

            private Il2CppTypeStruct* ThisArgPointer => (Il2CppTypeStruct*)(&(NativeClass->this_arg));

            public INativeTypeStruct ByValArg => UnityVersionHandler.Wrap(ByValArgPointer);

            public INativeTypeStruct ThisArg => UnityVersionHandler.Wrap(ThisArgPointer);

            public ref Il2CppImage* Image => ref NativeClass->image;

            public ref Il2CppClass* Parent => ref NativeClass->parent;

            public ref Il2CppClass* ElementClass => ref NativeClass->element_class;

            public ref Il2CppClass* CastClass => ref NativeClass->castClass;

            public ref Il2CppClass* Class => ref NativeClass->klass;

            public ref Il2CppMethodInfo** Methods => ref NativeClass->methods;
        }
    }
}