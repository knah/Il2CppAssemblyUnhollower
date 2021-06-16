using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Image
{
    public interface INativeImageStructHandler : INativeStructHandler
    {
        INativeImageStruct CreateNewImageStruct();
        unsafe INativeImageStruct Wrap(Il2CppImage* imagePointer);
        string GetName();
    }

    public interface INativeImageStruct : INativeStruct
    {
        unsafe Il2CppImage* ImagePointer { get; }

        unsafe ref Il2CppAssembly* Assembly { get; }

        ref byte Dynamic { get; }

        ref IntPtr Name { get; }

        bool HasNameNoExt { get; }

        ref IntPtr NameNoExt { get; }
    }
}