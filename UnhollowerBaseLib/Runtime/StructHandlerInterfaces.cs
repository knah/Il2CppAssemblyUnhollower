using System;

namespace UnhollowerBaseLib.Runtime
{
    public interface INativeStructHandler {}
    
    public interface INativeStruct
    {
        IntPtr Pointer { get; }
    }
    
    public interface INativeClassStructHandler : INativeStructHandler
    {
        INativeClassStruct CreateNewClassStruct(int vTableSlots);
        unsafe INativeClassStruct Wrap(Il2CppClass* classPointer);
    }

    public interface INativeClassStruct: INativeStruct
    {
        unsafe Il2CppClass* ClassPointer { get; }
        IntPtr VTable { get; }

        ref uint InstanceSize { get; }
        ref ushort VtableCount { get; }
        ref int NativeSize { get; }
        ref uint ActualSize { get; }
        ref ushort MethodCount { get; }
        ref Il2CppClassAttributes Flags { get; }
        
        bool ValueType { get; set; }
        bool EnumType { get; set; }
        bool IsGeneric { get; set; }
        bool Initialized { get; set; }
        bool InitializedAndNoError { get; set; }
        bool SizeInited { get; set; }
        bool HasFinalize { get; set; }
        bool IsVtableInitialized { get; set; }

        ref IntPtr Name { get; }
        ref IntPtr Namespace { get; }

        ref Il2CppTypeStruct ByValArg { get; }
        ref Il2CppTypeStruct ThisArg { get; }

        unsafe ref Il2CppImage* Image { get; }
        unsafe ref Il2CppClass* Parent { get; }
        unsafe ref Il2CppClass* ElementClass { get; }
        unsafe ref Il2CppClass* CastClass { get; }
        unsafe ref Il2CppClass* Class { get; }

        unsafe ref Il2CppMethodInfo** Methods { get; }
    }

    public interface INativeImageStructHandler : INativeStructHandler
    {
        INativeImageStruct CreateNewImageStruct();
        unsafe INativeImageStruct Wrap(Il2CppImage* classPointer);
    }

    public interface INativeImageStruct : INativeStruct
    {
        unsafe Il2CppImage* ImagePointer { get; }
        
        unsafe ref Il2CppAssembly* Assembly { get; }
        
        ref byte Dynamic { get; }
        
        ref IntPtr Name { get; }
        
        ref IntPtr NameNoExt { get; }
    }
}
