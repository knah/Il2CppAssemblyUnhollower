using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific
{
    [UnityVersionHandler("2018.1.0")]
    public class Unity2018_1
    {
        public static bool WorksOn(Version v)
        {
            return v.Major == 2018 && v.Minor == 1;
        }
        
        public class NativeClassStructHandler : INativeClassStructHandler
        {
            public unsafe INativeClassStruct CreateNewClassStruct(int vTableSlots)
            {
                var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppClassU2018_1>() +
                                                   Marshal.SizeOf<VirtualInvokeData>() * vTableSlots);

                *(Il2CppClassU2018_1*) pointer = default;

                return new NativeClassStructWrapper(pointer);
            }

            public unsafe INativeClassStruct Wrap(Il2CppClass* classPointer)
            {
                return new NativeClassStructWrapper((IntPtr) classPointer);
            }

            [StructLayout(LayoutKind.Sequential)]
            private unsafe struct Il2CppClassU2018_1
            {
                // The following fields are always valid for a Il2CppClass structure
                public Il2CppImage* image; // const
                public IntPtr gc_desc;
                public IntPtr name;                // const char*
                public IntPtr namespaze;           // const char*
                public Il2CppTypeStruct byval_arg; // not const, no ptr
                public Il2CppTypeStruct this_arg;  // not const, no ptr
                public Il2CppClass* element_class; // not const
                public Il2CppClass* castClass;     // not const
                public Il2CppClass* declaringType; // not const
                public Il2CppClass* parent;        // not const
                public /*Il2CppGenericClass**/ IntPtr generic_class;

                public                     /*Il2CppTypeDefinition**/
                    IntPtr typeDefinition; // const; non-NULL for Il2CppClass's constructed from type defintions

                public /*Il2CppInteropData**/ IntPtr interopData; // const

                public Il2CppClass* klass; // not const; hack to pretend we are a MonoVTable. Points to ourself
                // End always valid fields

                // The following fields need initialized before access. This can be done per field or as an aggregate via a call to Class::Init
                public FieldInfo* fields;                                  // Initialized in SetupFields
                public EventInfo* events;                                  // const; Initialized in SetupEvents
                public PropertyInfo* properties;                           // const; Initialized in SetupProperties
                public Il2CppMethodInfo** methods;                         // const; Initialized in SetupMethods
                public Il2CppClass** nestedTypes;                          // not const; Initialized in SetupNestedTypes
                public Il2CppClass** implementedInterfaces;                // not const; Initialized in SetupInterfaces
                public Il2CppRuntimeInterfaceOffsetPair* interfaceOffsets; // not const; Initialized in Init
                public IntPtr static_fields;                               // not const; Initialized in Init

                public /*Il2CppRGCTXData**/ IntPtr rgctx_data; // const; Initialized in Init

                // used for fast parent checks
                public Il2CppClass** typeHierarchy; // not const; Initialized in SetupTypeHierachy
                // End initialization required fields

                public uint cctor_started;

                public uint cctor_finished;

                /*ALIGN_TYPE(8)*/
                private ulong cctor_thread;

                // Remaining fields are always valid except where noted
                public /*GenericContainerIndex*/ int genericContainerIndex;
                public int customAttributeIndex;
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
                public byte packingSize;

                // this is critical for performance of Class::InitFromCodegen. Equals to initialized && !has_initialization_error at all times.
                // Use Class::UpdateInitializedAndNoError to update
                public byte bitfield_1;
                /*
                uint8_t valuetype : 1;
                uint8_t initialized : 1;
                uint8_t enumtype : 1;
                uint8_t is_generic : 1;
                uint8_t has_references : 1;
                uint8_t init_pending : 1;
                uint8_t size_inited : 1;
                uint8_t has_finalize : 1;*/

                public byte bitfield_2;
                /*uint8_t has_cctor : 1;
                uint8_t is_blittable : 1;
                uint8_t is_import_or_windows_runtime : 1;
                uint8_t is_vtable_initialized : 1;*/

                //VirtualInvokeData vtable[IL2CPP_ZERO_LEN_ARRAY];
            }

            private unsafe class NativeClassStructWrapper : INativeClassStruct
            {
                public NativeClassStructWrapper(IntPtr pointer)
                {
                    Pointer = pointer;
                }

                public IntPtr Pointer { get; }
                public Il2CppClass* ClassPointer => (Il2CppClass*) Pointer;

                public IntPtr VTable => IntPtr.Add(Pointer, Marshal.SizeOf<Il2CppClassU2018_1>());

                private Il2CppClassU2018_1* NativeClass => (Il2CppClassU2018_1*) ClassPointer;

                public ref uint InstanceSize => ref NativeClass->instance_size;

                public ref ushort VtableCount => ref NativeClass->vtable_count;

                public ref int NativeSize => ref NativeClass->native_size;

                public ref uint ActualSize => ref NativeClass->actualSize;

                public ref ushort MethodCount => ref NativeClass->method_count;

                private static int bitfield1offset =
                    Marshal.OffsetOf<Il2CppClassU2018_1>(nameof(Il2CppClassU2018_1.bitfield_1)).ToInt32();
                
                private static int bitfield2offset =
                    Marshal.OffsetOf<Il2CppClassU2018_1>(nameof(Il2CppClassU2018_1.bitfield_2)).ToInt32(); 
                
                public bool ValueType
                {
                    get => this.CheckBit(bitfield1offset, 0);
                    set => this.SetBit(bitfield1offset, 0, value);
                }

                public bool EnumType
                {
                    get => this.CheckBit(bitfield1offset, 2);
                    set => this.SetBit(bitfield1offset, 2, value);
                }

                public bool IsGeneric
                {
                    get => this.CheckBit(bitfield1offset, 3);
                    set => this.SetBit(bitfield1offset, 3, value);
                }

                public bool Initialized
                {
                    get => this.CheckBit(bitfield1offset, 1);
                    set => this.SetBit(bitfield1offset, 1, value);
                }

                // Not present
                public bool InitializedAndNoError
                {
                    get => true;
                    set { }
                }

                public bool SizeInited
                {
                    get => this.CheckBit(bitfield1offset, 6);
                    set => this.SetBit(bitfield1offset, 6, value);
                }

                public bool HasFinalize
                {
                    get => this.CheckBit(bitfield1offset, 7);
                    set => this.SetBit(bitfield1offset, 7, value);
                }

                public bool IsVtableInitialized
                {
                    get => this.CheckBit(bitfield2offset, 3);
                    set => this.SetBit(bitfield2offset, 3, value);
                }

                public ref Il2CppClassAttributes Flags => ref NativeClass->flags;

                public ref IntPtr Name => ref NativeClass->name;

                public ref IntPtr Namespace => ref NativeClass->namespaze;

                public ref Il2CppTypeStruct ByValArg => ref NativeClass->byval_arg;

                public ref Il2CppTypeStruct ThisArg => ref NativeClass->this_arg;

                public ref Il2CppImage* Image => ref NativeClass->image;

                public ref Il2CppClass* Parent => ref NativeClass->parent;

                public ref Il2CppClass* ElementClass => ref NativeClass->element_class;

                public ref Il2CppClass* CastClass => ref NativeClass->castClass;

                public ref Il2CppClass* Class => ref NativeClass->klass;

                public ref Il2CppMethodInfo** Methods => ref NativeClass->methods;
            }
        }
        
        public unsafe class NativeImageStructHandler : INativeImageStructHandler
        {
            public INativeImageStruct CreateNewImageStruct()
            {
                var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppImageU2018_1>());
                *(Il2CppImageU2018_1*) pointer = default;

                return new NativeImageStruct(pointer);
            }

            public INativeImageStruct Wrap(Il2CppImage* imagePointer)
            {
                return new NativeImageStruct((IntPtr) imagePointer);
            }

            private struct Il2CppImageU2018_1
            {
                public IntPtr name;      // const char*
                public IntPtr nameNoExt; // const char*
                public Il2CppAssembly* assembly;

                public int typeStart;
                public uint typeCount;

                public int exportedTypeStart;
                public uint exportedTypeCount;
                
                public int entryPointIndex;

                public /*Il2CppNameToTypeDefinitionIndexHashTable **/ IntPtr nameToClassHashTable;

                public uint token;
                public byte dynamic;
            }

            private class NativeImageStruct : INativeImageStruct
            {
                public NativeImageStruct(IntPtr pointer)
                {
                    Pointer = pointer;
                }

                public IntPtr Pointer { get; }

                public Il2CppImage* ImagePointer => (Il2CppImage*) Pointer;

                private Il2CppImageU2018_1* NativeImage => (Il2CppImageU2018_1*) ImagePointer;

                public ref Il2CppAssembly* Assembly => ref NativeImage->assembly;

                public ref byte Dynamic => ref NativeImage->dynamic;

                public ref IntPtr Name => ref NativeImage->name;

                public ref IntPtr NameNoExt => ref NativeImage->nameNoExt;
            }
        }

        public unsafe class NativeMethodStructHandler : INativeMethodStructHandler
        {
            public INativeMethodStruct CreateNewMethodStruct()
            {
                var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppMethodInfoU2018_1>());
                *(Il2CppMethodInfoU2018_1*) pointer = default;
                
                return new NativeMethodInfoStructWrapper(pointer);
            }

            public INativeMethodStruct Wrap(Il2CppMethodInfo* methodPointer)
            {
                return new NativeMethodInfoStructWrapper((IntPtr) methodPointer);
            }

            public Il2CppParameterInfo*[] CreateNewParameterInfoArray(int paramCount)
            {
                var ptr = (Il2CppParameterInfoU2018_1*) Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppParameterInfoU2018_1>() * paramCount);
                var res = new Il2CppParameterInfo*[paramCount];
                for (var i = 0; i < paramCount; i++)
                {
                    ptr[i] = default;
                    res[i] = (Il2CppParameterInfo*) &ptr[i];
                }
                return res;
            }

            public INativeParameterInfoStruct Wrap(Il2CppParameterInfo* paramInfoPointer)
            {
                return new NativeParamInfoStructWrapper((IntPtr) paramInfoPointer);
            }

            public IntPtr GetMethodFromReflection(IntPtr method)
            {
                return ((Il2CppReflectionMethodU2018_1*) method)->methodInfo;
            }
            
            private struct Il2CppReflectionMethodU2018_1
            {
                public IntPtr klassVtable;
                public IntPtr monitor;
                public IntPtr methodInfo;
                public IntPtr name;
                public IntPtr refType;
            }

            private struct Il2CppParameterInfoU2018_1
            {
                public IntPtr name;
                public int position;
                public uint token;
                public int customAttributeIndex;
                public Il2CppTypeStruct* parameter_type;
            }

            private struct Il2CppMethodInfoU2018_1
            {
                public IntPtr methodPointer;
                public IntPtr invoker_method;
                public IntPtr name;
                public Il2CppClass* klass;
                public Il2CppTypeStruct* return_type;
                public Il2CppParameterInfo* parameters;
                public IntPtr someRtData;
                public IntPtr someGenericData;
                public int customAttributeIndex;
                public uint token;
                public Il2CppMethodFlags flags;
                public Il2CppMethodImplFlags iflags;
                public ushort slot;
                public byte parameters_count;
                public MethodInfoExtraFlags extra_flags;
            }

            private class NativeParamInfoStructWrapper : INativeParameterInfoStruct
            {
                public NativeParamInfoStructWrapper(IntPtr pointer)
                {
                    Pointer = pointer;
                }
                
                public IntPtr Pointer { get; }

                public Il2CppParameterInfo* ParameterInfoPointer => (Il2CppParameterInfo*) Pointer;

                public Il2CppParameterInfoU2018_1* NativeParameter => (Il2CppParameterInfoU2018_1*) Pointer;

                public ref IntPtr Name => ref NativeParameter->name;

                public ref int Position => ref NativeParameter->position;

                public ref uint Token => ref NativeParameter->token;

                public ref Il2CppTypeStruct* ParameterType => ref NativeParameter->parameter_type;
            }

            private class NativeMethodInfoStructWrapper : INativeMethodStruct
            {
                public NativeMethodInfoStructWrapper(IntPtr pointer)
                {
                    Pointer = pointer;
                }

                public int StructSize => Marshal.SizeOf<Il2CppMethodInfoU2018_1>();
                
                public IntPtr Pointer { get; }
                
                public Il2CppMethodInfo* MethodInfoPointer => (Il2CppMethodInfo*) Pointer;

                private Il2CppMethodInfoU2018_1* NativeMethod => (Il2CppMethodInfoU2018_1*) Pointer;

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
}