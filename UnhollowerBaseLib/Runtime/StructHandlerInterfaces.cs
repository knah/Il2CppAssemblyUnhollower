using System;

namespace UnhollowerBaseLib.Runtime
{
    public interface INativeStructHandler {}
    
    public interface INativeStruct
    {
        IntPtr Pointer { get; }
    }
}
