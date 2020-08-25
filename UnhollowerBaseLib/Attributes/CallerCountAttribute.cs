using System;

namespace UnhollowerBaseLib.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CallerCountAttribute : Attribute
    {
        public readonly int Count;

        public CallerCountAttribute(int count)
        {
            Count = count;
        }
    }
}