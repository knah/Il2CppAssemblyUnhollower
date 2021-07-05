using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Assembly
{
    public interface INativeAssemblyStructHandler : INativeStructHandler
    {
        INativeAssemblyStruct CreateNewAssemblyStruct();
        unsafe INativeAssemblyStruct Wrap(Il2CppAssembly* assemblyPointer);
        IntPtr il2cpp_assembly_get_name(IntPtr assembly);
#if DEBUG
        string GetName();
#endif
    }

    public interface INativeAssemblyStruct : INativeStruct
    {
        unsafe Il2CppAssembly* AssemblyPointer { get; }

        unsafe ref Il2CppImage* Image { get; }

        ref IntPtr Name { get; }

        ref int Major { get; }

        ref int Minor { get; }

        ref int Build { get; }

        ref int Revision { get; }
    }
}
