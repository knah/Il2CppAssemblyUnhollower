using System;

namespace UnhollowerBaseLib
{
    public class AlsoInitializeAttribute : Attribute
    {
        public readonly Type LinkedType;

        public AlsoInitializeAttribute(Type linkedType)
        {
            LinkedType = linkedType;
        }
    }
}