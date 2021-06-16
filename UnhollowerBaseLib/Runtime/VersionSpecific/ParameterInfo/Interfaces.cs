using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.ParameterInfo
{
    public interface INativeParameterInfoStructHandler : INativeStructHandler
    {
        unsafe Il2CppParameterInfo*[] CreateNewParameterInfoArray(int paramCount);
        unsafe INativeParameterInfoStruct Wrap(Il2CppParameterInfo* paramInfoPointer);
        string GetName();
    }
    
    public interface INativeParameterInfoStruct : INativeStruct
    {
        unsafe Il2CppParameterInfo* ParameterInfoPointer { get; }
        ref IntPtr Name { get; }
        ref int Position { get; }
        ref uint Token { get; }
        unsafe ref Il2CppTypeStruct* ParameterType { get; }
    }
}