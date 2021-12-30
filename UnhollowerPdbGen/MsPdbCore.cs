using System;
using System.Runtime.InteropServices;
using System.Text;

// Source/reference: https://github.com/microsoft/microsoft-pdb, MIT license
namespace UnhollowerPdbGen
{
    enum PDBErrors: int {
        EC_OK,                          // no problem
        EC_USAGE,                       // invalid parameter or call order
        EC_OUT_OF_MEMORY,               // out of heap
        EC_FILE_SYSTEM,                 // "pdb name", can't write file, out of disk, etc.
        EC_NOT_FOUND,                   // "pdb name", PDB file not found
        EC_INVALID_SIG,                 // "pdb name", PDB::OpenValidate() and its clients only
        EC_INVALID_AGE,                 // "pdb name", PDB::OpenValidate() and its clients only
        EC_PRECOMP_REQUIRED,            // "obj name", Mod::AddTypes() only
        EC_OUT_OF_TI,                   // "pdb name", TPI::QueryTiForCVRecord() only
        EC_NOT_IMPLEMENTED,             // -
        EC_V1_PDB,                      // "pdb name", PDB::Open* only (obsolete)
        EC_UNKNOWN_FORMAT = EC_V1_PDB,  // pdb can't be opened because it has newer versions of stuff
        EC_FORMAT,                      // accessing pdb with obsolete format
        EC_LIMIT,
        EC_CORRUPT,                     // cv info corrupt, recompile mod
        EC_TI16,                        // no 16-bit type interface present
        EC_ACCESS_DENIED,               // "pdb name", PDB file read-only
        EC_ILLEGAL_TYPE_EDIT,           // trying to edit types in read-only mode
        EC_INVALID_EXECUTABLE,          // not recogized as a valid executable
        EC_DBG_NOT_FOUND,               // A required .DBG file was not found
        EC_NO_DEBUG_INFO,               // No recognized debug info found
        EC_INVALID_EXE_TIMESTAMP,       // Invalid timestamp on Openvalidate of exe
        EC_CORRUPT_TYPEPOOL,            // A corrupted type record was found in a PDB
        EC_DEBUG_INFO_NOT_IN_PDB,       // returned by OpenValidateX
        EC_RPC,                         // Error occured during RPC
        EC_UNKNOWN,                     // Unknown error
        EC_BAD_CACHE_PATH,              // bad cache location specified with symsrv
        EC_CACHE_FULL,                  // symsrv cache is full
        EC_TOO_MANY_MOD_ADDTYPE,        // Addtype is called more then once per mod
        EC_MAX
    }
    
    [Flags]
    enum CV_PUBSYMFLAGS_e: int
    {
        cvpsfNone     = 0,
        cvpsfCode     = 0x00000001,
        cvpsfFunction = 0x00000002,
        cvpsfManaged  = 0x00000004,
        cvpsfMSIL     = 0x00000008,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PdbPtr
    {
        public IntPtr InnerPtr;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct DbiPtr
    {
        public IntPtr InnerPtr;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ModPtr
    {
        public IntPtr InnerPtr;
    }
    
    public static unsafe class MsPdbCore
    {
        private const string dllName = "mspdbcore.dll";
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool PDBOpen2W(char* wszPDB, byte* szMode, out PDBErrors pec, char* wszError, nuint cchErrMax, out PdbPtr pppdb);

        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool PDBCommit(PdbPtr ppdb);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool PDBOpenDBI(PdbPtr ppdb, byte* szMode, byte* szTarget, out DbiPtr ppdbi);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool DBIOpenModW(DbiPtr pdbi, char* szModule, char* szFile, out ModPtr ppmod);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool DBIAddPublic2(DbiPtr pdbi, byte* szPublic, ushort isect, int off, CV_PUBSYMFLAGS_e cvpsf=0);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool ModAddPublic2(ModPtr pmod, byte* szPublic, ushort isect, int off, CV_PUBSYMFLAGS_e cvpsf=0);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool DBIAddSec(DbiPtr pdbi, ushort isect, ushort flags, int off, int cb);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool ModClose(ModPtr ppdb);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool DBIClose(DbiPtr ppdb);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool PDBClose(PdbPtr ppdb);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool PDBQuerySignature2(PdbPtr ppdb, out Guid guid);
        
        [DllImport(dllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern uint PDBQueryAge(PdbPtr ppdb);


        internal static bool PDBOpen2W(string wszPDB, string szMode, out PDBErrors pec, out string error, out PdbPtr pppdb)
        {
            wszPDB += '\0';
            szMode += '\0';

            var chars = wszPDB.ToCharArray();
            var bytes = Encoding.UTF8.GetBytes(szMode);
            var errorChars = new char[2048];
            bool result = false;
            
            fixed(char* cp = chars)
            fixed(byte* bp = bytes)
            fixed (char* ep = errorChars)
                result = PDBOpen2W(cp, bp, out pec, ep, (nuint) errorChars.Length, out pppdb);

            var firstZero = Array.IndexOf(errorChars, '\0');
            error = new string(errorChars, 0, firstZero);

            return result;
        }

        internal static bool PDBOpenDBI(PdbPtr ppdb, string szMode, string szTarget, out DbiPtr ppdbi)
        {
            szMode += '\0';
            szTarget += '\0';
            
            fixed(byte* mb = Encoding.UTF8.GetBytes(szMode))
            fixed (byte* tb = Encoding.UTF8.GetBytes(szTarget))
                return PDBOpenDBI(ppdb, mb, tb, out ppdbi);
        }

        internal static bool DBIOpenModW(DbiPtr pdbi, string szModule, string szFile, out ModPtr ppmod)
        {
            szFile += '\0';
            szModule += '\0';
            
            fixed(char* fp = szFile)
            fixed (char* mp = szModule)
                return DBIOpenModW(pdbi, mp, fp, out ppmod);
        }

        internal static bool ModAddPublic2(ModPtr pmod, string szPublic, ushort isect, int off, CV_PUBSYMFLAGS_e cvpsf = 0)
        {
            szPublic += '\0';
            fixed (byte* mb = Encoding.UTF8.GetBytes(szPublic))
                return ModAddPublic2(pmod, mb, isect, off, cvpsf);
        }
    }
}