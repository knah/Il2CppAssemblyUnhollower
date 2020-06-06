using System;

namespace UnhollowerBaseLib.Attributes
{
    /// <summary>
    /// This attribute indicates that the target should not be exposed to IL2CPP in injected classes
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor)]
    public class HideFromIl2CppAttribute : Attribute
    {
    }
}