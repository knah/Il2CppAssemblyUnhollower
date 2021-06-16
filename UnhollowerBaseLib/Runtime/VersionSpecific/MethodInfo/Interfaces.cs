using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.MethodInfo
{
    public interface INativeMethodInfoStructHandler : INativeStructHandler
    {
        INativeMethodInfoStruct CreateNewMethodStruct();
        unsafe INativeMethodInfoStruct Wrap(Il2CppMethodInfo* methodPointer);
        IntPtr GetMethodFromReflection(IntPtr method);
        IntPtr CopyMethodInfoStruct(IntPtr origMethodInfo);
        string GetName();
    }


    public interface INativeMethodInfoStruct : INativeStruct
    {
        int StructSize { get; }
        unsafe Il2CppMethodInfo* MethodInfoPointer { get; }
        ref IntPtr Name { get; }
        ref ushort Slot { get; }
        ref IntPtr MethodPointer { get; }
        unsafe ref Il2CppClass* Class { get; }
        ref IntPtr InvokerMethod { get; }
        unsafe ref Il2CppTypeStruct* ReturnType { get; }
        ref Il2CppMethodFlags Flags { get; }
        ref byte ParametersCount { get; }
        unsafe ref Il2CppParameterInfo* Parameters { get; }
        ref MethodInfoExtraFlags ExtraFlags { get; }
    }
}