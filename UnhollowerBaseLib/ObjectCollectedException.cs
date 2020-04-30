using System;

namespace UnhollowerBaseLib
{
    public class ObjectCollectedException : Exception
    {
        public ObjectCollectedException(string message) : base(message)
        {
        }
    }
}