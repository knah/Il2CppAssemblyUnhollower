using System;
using System.Runtime.InteropServices;

namespace UnhollowerBaseLib.Runtime.VersionSpecific.Exception
{
    [ApplicableToUnityVersionsSince("5.3.0")]
    public unsafe class NativeExceptionStructHandler_16_0 : INativeExceptionStructHandler
    {
        public INativeExceptionStruct CreateNewExceptionStruct()
        {
            var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<Il2CppException_16_0>());

            *(Il2CppException_16_0*)pointer = default;

            return new NativeEventInfoStruct(pointer);
        }

        public INativeExceptionStruct Wrap(Il2CppException* exceptionPointer)
        {
            if ((IntPtr)exceptionPointer == IntPtr.Zero) return null;
            else return new NativeEventInfoStruct((IntPtr)exceptionPointer);
        }

#if DEBUG
        public string GetName() => "NativeExceptionStructHandler_16_0";
#endif

        [StructLayout(LayoutKind.Sequential)]
        internal struct Il2CppException_16_0
        {
            public Il2CppObject @object;
            public IntPtr /* Il2CppArray* */ trace_ips;
            public IntPtr /* Il2CppObject* */ inner_ex;
            public IntPtr /* Il2CppString* */ message;
            public IntPtr /* Il2CppString* */ help_link;
            public IntPtr /* Il2CppString* */ class_name;
            public IntPtr /* Il2CppString* */ stack_trace;
            public IntPtr /* Il2CppString* */ remote_stack_trace;
            public int remote_stack_index;
            public int hresult;
            public IntPtr /* Il2CppString* */ source;
            public IntPtr /* Il2CppObject* */ _data;
        }

        internal class NativeEventInfoStruct : INativeExceptionStruct
        {
            public NativeEventInfoStruct(IntPtr pointer)
            {
                Pointer = pointer;
            }

            public IntPtr Pointer { get; }

            public Il2CppException* ExceptionPointer => (Il2CppException*)Pointer;

            private Il2CppException_16_0* NativeException => (Il2CppException_16_0*)Pointer;

            private Il2CppException* dummyInnerException = (Il2CppException*)IntPtr.Zero;

            public ref Il2CppException* InnerException => ref dummyInnerException;

            public ref IntPtr Message => ref NativeException->message;

            public ref IntPtr HelpLink => ref NativeException->help_link;

            public ref IntPtr ClassName => ref NativeException->class_name;

            public ref IntPtr StackTrace => ref NativeException->stack_trace;

            public ref IntPtr RemoteStackTrace => ref NativeException->remote_stack_trace;
        }
    }
}
