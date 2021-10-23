using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Marshalling;

namespace UnhollowerRuntimeLib.XrefScans
{
    public readonly struct XrefInstance
    {
        public readonly XrefType Type;
        public readonly IntPtr Pointer;
        public readonly IntPtr FoundAt;

        public XrefInstance(XrefType type, IntPtr pointer, IntPtr foundAt)
        {
            Type = type;
            Pointer = pointer;
            FoundAt = foundAt;
        }

        internal XrefInstance RelativeToBase(long baseAddress)
        {
            return new XrefInstance(Type, (IntPtr) ((long) Pointer - baseAddress), (IntPtr) ((long) FoundAt - baseAddress));
        }

        //public Il2CppObjectBase ReadAsObject()
        public Il2CppSystem.Object ReadAsObject()//todo: replace with Il2CppObjectBase
        {
            if (Type != XrefType.Global) throw new InvalidOperationException("Can't read non-global xref as object");

            var valueAtPointer = Marshal.ReadIntPtr(Pointer);
            //return MarshallingUtils.MarshalObjectFromPointer(valueAtPointer);//todo: uncomment and remove subsequent code
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