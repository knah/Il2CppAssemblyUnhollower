using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnhollowerRuntimeLib.XrefScans
{
    public readonly struct XrefInstance
    {
        public readonly XrefType Type;
        public readonly IntPtr Pointer;

        public XrefInstance(XrefType type, IntPtr pointer)
        {
            Type = type;
            Pointer = pointer;
        }

        public Il2CppSystem.Object ReadAsObject()
        {
            if (Type != XrefType.Global) throw new InvalidOperationException("Can't read non-global xref as object");

            var valueAtPointer = Marshal.ReadIntPtr(Pointer);
            if (valueAtPointer == IntPtr.Zero)
                return null;
            
            return new Il2CppSystem.Object(valueAtPointer);
        }

        public MethodBase TryResolve()
        {
            if (Type != XrefType.Method) throw new InvalidOperationException("Can't resolve non-method xrefs");

            return XrefScanMethodDb.TryResolvePointer(Pointer);
        }
    }
}