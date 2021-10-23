using System;

namespace UnhollowerBaseLib.Attributes
{
    public class NativeTypeTokenAttribute : Attribute
    {
        public string AssemblyName;
        public uint Token;
    }
}
