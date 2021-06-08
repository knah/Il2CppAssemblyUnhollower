using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.MethodInfo
{
    public interface INativeMethodStructHandler : INativeStructHandler
    {
        INativeMethodStruct CreateNewMethodStruct();
        unsafe INativeMethodStruct Wrap(Il2CppMethodInfo* methodPointer);
        IntPtr GetMethodFromReflection(IntPtr method);
        Type StructType { get; }
    }


    public interface INativeMethodStruct : INativeStruct
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