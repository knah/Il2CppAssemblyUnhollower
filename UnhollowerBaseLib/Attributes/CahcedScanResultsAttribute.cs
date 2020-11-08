using System;

namespace UnhollowerBaseLib.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class CachedScanResultsAttribute : Attribute
    {
        // Items that this method calls/uses
        public int XrefRangeStart;
        public int XrefRangeEnd;
        
        // Methods that call this method
        public int RefRangeStart;
        public int RefRangeEnd;

        // Data for metadata init call
        public long MetadataInitFlagRva;
        public long MetadataInitTokenRva;
    }
}