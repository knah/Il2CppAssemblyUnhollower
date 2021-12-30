using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace UnhollowerBaseLib.Maps
{
    public abstract class MethodAddressToTokenMapBase<TAssembly, TMethod> : IDisposable, IEnumerable<(long, TMethod?)>
    {
        public const int Magic = 0x4D544D55; // UMTM
        public const int Version = 1;
        public const string FileName = "MethodAddressToToken.db";
        
        private readonly MemoryMappedFile? myMapFile;
        private readonly MemoryMappedViewAccessor? myAccessor;
        
        private unsafe long* myPointers;
        private unsafe int* myValues;
        
        private readonly MethodAddressToTokenMapFileHeader myHeader;
        private readonly List<TAssembly?> myAssemblyList = new();

        protected readonly string myFilePath;

        public MethodAddressToTokenMapBase(string filePath)
        {
            myFilePath = filePath;
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
            
            var offset = Marshal.SizeOf<MethodAddressToTokenMapFileHeader>();
            using var reader = new BinaryReader(myMapFile.CreateViewStream(offset, 0, MemoryMappedFileAccess.Read), Encoding.UTF8, false);
            for (var i = 0; i < myHeader.NumAssemblies; i++)
            {
                var assemblyName = reader.ReadString();
                myAssemblyList.Add(LoadAssembly(assemblyName));
            }

            unsafe
            {
                byte* pointersPointer = null;

                myAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointersPointer);

                myPointers = (long*) (pointersPointer + myHeader.DataOffset);
                myValues = (int*) (pointersPointer + myHeader.DataOffset + myHeader.NumMethods * 8);
            }
        }

        protected abstract TAssembly? LoadAssembly(string assemblyName);

        private void ReleaseUnmanagedResources()
        {
            myAccessor?.SafeMemoryMappedViewHandle.ReleasePointer();
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

        ~MethodAddressToTokenMapBase()
        {
            Dispose(false);
        }

        public unsafe TMethod? Lookup(long parsedRva)
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
                return default;

            var dataToken = myValues[left * 2];
            var assemblyIdx = myValues[left * 2 + 1];

            return ResolveMethod(myAssemblyList[assemblyIdx], dataToken);
        }

        protected abstract TMethod? ResolveMethod(TAssembly? assembly, int token);
        
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<(long, TMethod?)> IEnumerable<(long, TMethod?)>.GetEnumerator() => GetEnumerator();

        public Enumerator GetEnumerator() => new(this);

        public class Enumerator : IEnumerator<(long, TMethod?)>
        {
            private readonly MethodAddressToTokenMapBase<TAssembly, TMethod> myMap;
            private int myOffset = -1;

            public Enumerator(MethodAddressToTokenMapBase<TAssembly, TMethod> map)
            {
                myMap = map;
            }

            public unsafe bool MoveNext()
            {
                myOffset++;
                if (myOffset < myMap.myHeader.NumMethods)
                {
                    var dataToken = myMap.myValues[myOffset * 2];
                    var assemblyIdx = myMap.myValues[myOffset * 2 + 1];
                    
                    Current = (myMap.myPointers[myOffset], myMap.ResolveMethod(myMap.myAssemblyList[assemblyIdx], dataToken));
                    
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                myOffset = -1;
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public (long, TMethod?) Current { get; private set; }
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct MethodAddressToTokenMapFileHeader
    {
        public int Magic;
        public int Version;
        public int NumAssemblies;
        public int NumMethods;
        public int DataOffset;
            
        // data is long[NumMethods] pointers, (int, int)[NumMethods] (tokens, assemblyIds)
    }
}