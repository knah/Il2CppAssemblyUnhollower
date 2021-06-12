using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Assembly
{
    public interface INativeAssemblyStructHandler : INativeStructHandler
    {
        INativeAssemblyStruct CreateNewAssemblyStruct();
        unsafe INativeAssemblyStruct Wrap(Il2CppAssembly* assemblyPointer);
    }

    public interface INativeAssemblyStruct : INativeStruct
    {
        unsafe Il2CppAssembly* AssemblyPointer { get; }

        unsafe ref Il2CppImage* Image { get; }

        ref IntPtr Name { get; }
    }
}
