using System;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Exception
{
    public interface INativeExceptionStructHandler : INativeStructHandler
    {
        INativeExceptionStruct CreateNewExceptionStruct();
        unsafe INativeExceptionStruct Wrap(Il2CppException* exceptionPointer);
#if DEBUG
        string GetName();
#endif
    }

    public interface INativeExceptionStruct : INativeStruct
    {
        unsafe Il2CppException* ExceptionPointer { get; }

        unsafe ref Il2CppException* InnerException { get; }

        INativeExceptionStruct InnerExceptionWrapped { get; }

        ref IntPtr Message { get; }

        ref IntPtr HelpLink { get; }

        ref IntPtr ClassName { get; }

        ref IntPtr StackTrace { get; }

        ref IntPtr RemoteStackTrace { get; }
    }
}
