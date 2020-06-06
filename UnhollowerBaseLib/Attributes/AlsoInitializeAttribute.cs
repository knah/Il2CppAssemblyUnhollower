using System;

namespace UnhollowerBaseLib.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class AlsoInitializeAttribute : Attribute
    {
        public readonly Type LinkedType;

        public AlsoInitializeAttribute(Type linkedType)
        {
            LinkedType = linkedType;
        }
    }
}