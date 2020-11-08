using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using UnhollowerRuntimeLib.XrefScans;

namespace UnhollowerBaseLib.Maps
{
    public class MethodXrefScanCache : IDisposable
    {
        public const int Magic = 0x43584D55; // UMXC
        public const int Version = 1;
        public const string FileName = "MethodXrefScanCache.db";
        
        private readonly MemoryMappedFile myMapFile;
        private readonly MemoryMappedViewAccessor myAccessor;
        
        private unsafe MethodData* myData;

        public readonly FileHeader Header;

        public MethodXrefScanCache(string filePath)
        {
            myMapFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);

            var headerView = myMapFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            myAccessor = headerView;

            headerView.Read(0, out Header);

            if (Header.Magic != Magic)
            {
                myMapFile.Dispose();
                throw new FileLoadException($"File magic mismatched for {filePath}; Expected {Magic:X}, got {Header.Magic:X}");
            }
            
            if (Header.Version != Version)
            {
                myMapFile.Dispose();
                throw new FileLoadException($"File version mismatched for {filePath}; Expected {Version}, got {Header.Version}");
            }
            
            var offset = Marshal.SizeOf<FileHeader>();

            unsafe
            {
                byte* pointersPointer = null;

                myAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointersPointer);

                myData = (MethodData*) (pointersPointer + offset);
            }
        }

        private void ReleaseUnmanagedResources()
        {
            myAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
            unsafe
            {
                myData = null;
            }
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                myAccessor?.Dispose();
                myMapFile?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MethodXrefScanCache()
        {
            Dispose(false);
        }

        internal unsafe ref MethodData GetAt(int index)
        {
            return ref *(myData + index);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FileHeader
        {
            public int Magic;
            public int Version;
            public long InitMethodMetadataRva;

            // data is MethodData[]
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MethodData
        {
            public long Address;
            public long FoundAt;
            public XrefType Type;

            public XrefInstance AsXrefInstance(long baseAddress)
            {
                return new XrefInstance(Type, (IntPtr) (baseAddress + Address), (IntPtr) (baseAddress + FoundAt));
            }

            public static MethodData FromXrefInstance(XrefInstance instance)
            {
                return new MethodData
                {
                    Address = (long) instance.Pointer,
                    FoundAt = (long) instance.FoundAt,
                    Type = instance.Type
                };
            }
        }
    }
}