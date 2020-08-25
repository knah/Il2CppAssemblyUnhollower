using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace UnhollowerBaseLib.Maps
{
    public class MethodAddressToTokenMap : IDisposable
    {
        public const int Magic = 0x4D544D55; // UMTM
        public const int Version = 1;
        public const string FileName = "MethodAddressToToken.db";
        
        private readonly MemoryMappedFile myMapFile;
        private readonly MemoryMappedViewAccessor myAccessor;
        
        private unsafe long* myPointers;
        private unsafe int* myValues;
        
        private readonly FileHeader myHeader;
        private readonly List<Assembly> myAssemblyList = new List<Assembly>();

        public MethodAddressToTokenMap(string filePath)
        {
            myMapFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);

            var headerView = myMapFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            myAccessor = headerView;

            headerView.Read(0, out myHeader);

            if (myHeader.Magic != Magic)
            {
                myMapFile.Dispose();
                throw new FileLoadException($"File magic mismatched for {filePath}; Expected {Magic:X}, got {myHeader.Magic:X}");
            }
            
            if (myHeader.Version != Version)
            {
                myMapFile.Dispose();
                throw new FileLoadException($"File version mismatched for {filePath}; Expected {Version}, got {myHeader.Version}");
            }
            
            var offset = Marshal.SizeOf<FileHeader>();
            using var reader = new BinaryReader(myMapFile.CreateViewStream(offset, 0, MemoryMappedFileAccess.Read), Encoding.UTF8, false);
            for (var i = 0; i < myHeader.NumAssemblies; i++)
            {
                var assemblyName = reader.ReadString();
                myAssemblyList.Add(Assembly.Load(assemblyName));
            }

            unsafe
            {
                byte* pointersPointer = null;

                myAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointersPointer);

                myPointers = (long*) (pointersPointer + myHeader.DataOffset);
                myValues = (int*) (pointersPointer + myHeader.DataOffset + myHeader.NumMethods * 8);
            }
        }

        private void ReleaseUnmanagedResources()
        {
            myAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
            unsafe
            {
                myPointers = null;
                myValues = null;
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

        ~MethodAddressToTokenMap()
        {
            Dispose(false);
        }

        public unsafe MethodBase Lookup(long parsedRva)
        {
            var left = 0;
            var right = myHeader.NumMethods;

            while (right - left > 1)
            {
                var mid = (left + right) / 2;
                var pointerAt = myPointers[mid];
                if (pointerAt > parsedRva)
                    right = mid;
                else
                    left = mid;
            }

            if (myPointers[left] != parsedRva)
                return null;

            var dataToken = myValues[left * 2];
            var assemblyIdx = myValues[left * 2 + 1];

            return myAssemblyList[assemblyIdx]?.ManifestModule.ResolveMethod(dataToken);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FileHeader
        {
            public int Magic;
            public int Version;
            public int NumAssemblies;
            public int NumMethods;
            public int DataOffset;
            
            // data is long[NumMethods] pointers, (int, int)[NumMethods] (tokens, assemblyIds)
        }
    }
}