using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnhollowerBaseLib.Runtime;

namespace UnhollowerBaseLib.Maps
{
	public class TypeTokensMap : IDisposable
    {
        public const int Magic = 0x4D545455; // UTTM
        public const int Version = 1;
        public const string FileName = "TypeTokens.db";

        private readonly MemoryMappedFile myMapFile;
        private readonly MemoryMappedViewAccessor myAccessor;

        private unsafe int* myValues;

        private readonly FileHeader myHeader;
        private readonly Dictionary<IntPtr, (Assembly?, int rangeStart, int rangeEnd, int tokensOffset)> myAssemblyMap = new();

        private readonly ConcurrentDictionary<IntPtr, Type> myClassPointerToTypeMap = new();

        public TypeTokensMap(string filePath)
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
                var il2CppName = reader.ReadString();
                var nativeAssemblyPointer = IL2CPP.GetImagePointer(il2CppName);
                var managedAssemblyName = reader.ReadString();
                var rangeStart = reader.ReadInt32();
                var rangeEnd = reader.ReadInt32();
                var tokenOffset = reader.ReadInt32();
                Assembly? assembly = null;
                try
                {
                    assembly = Assembly.Load(managedAssemblyName);
                }
                catch (FileNotFoundException ex)
                {
                    LogSupport.Trace($"Assembly {managedAssemblyName} not found for type-to-token map; it probably was ignored; {ex}");
                }
                myAssemblyMap[nativeAssemblyPointer] = (assembly, rangeStart, rangeEnd, tokenOffset);
            }

            unsafe
            {
                byte* pointersPointer = null;

                myAccessor.SafeMemoryMappedViewHandle.AcquirePointer(ref pointersPointer);

                myValues = (int*)(pointersPointer + myHeader.DataOffset);
            }
        }

        private void ReleaseUnmanagedResources()
        {
            myAccessor.SafeMemoryMappedViewHandle.ReleasePointer();
            unsafe
            {
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

        ~TypeTokensMap()
        {
            Dispose(false);
        }

        public void RegisterRuntimeInjectedType(IntPtr nativeType, Type managedType)
        {
            myClassPointerToTypeMap[nativeType] = managedType;
        }

        public unsafe Type? LookupByClass(IntPtr clazz)
        {
            if (myClassPointerToTypeMap.TryGetValue(clazz, out var type))
                return type;

            if (IL2CPP.il2cpp_class_is_inflated(clazz))
            {
                var il2CppType = Il2CppSystem.Type.internal_from_handle(IL2CPP.il2cpp_class_get_type(clazz));
                var genericDef = il2CppType.GetGenericTypeDefinition();
                var genericDefMonoSide = LookupByClass(IL2CPP.il2cpp_class_from_type(genericDef.TypeHandle.value));
                if (genericDefMonoSide == null) return null;
                var genericParamsMonoSide = genericDef.GetGenericArguments().Select(it => LookupByClass(IL2CPP.il2cpp_class_from_type(it.TypeHandle.value))).ToArray();
                if (genericParamsMonoSide.Any(it => it == null))
                    return null;

                var result = genericDefMonoSide.MakeGenericType(genericParamsMonoSide);
                return myClassPointerToTypeMap[clazz] = result;
            }

            // pointer and byref are Type but not Class
            var nativeClassStruct = UnityVersionHandler.Wrap((Il2CppClass*)clazz);
            var byValTypeType = nativeClassStruct.ByValArg.Type;
            if (byValTypeType == Il2CppTypeEnum.IL2CPP_TYPE_SZARRAY || byValTypeType == Il2CppTypeEnum.IL2CPP_TYPE_ARRAY)
            {
                if (nativeClassStruct.Rank > 1)
                {
                    return myClassPointerToTypeMap[clazz] = typeof(Il2CppSystem.Object); // higher-rank arrays are not supported currently
                }

                var elementClazz = (IntPtr)nativeClassStruct.ElementClass;
                var elementType = LookupByClass(elementClazz);
                if (elementType == null) return null;

                Type appropriateArrayType;
                if (elementType.IsValueType)
                    appropriateArrayType = typeof(Il2CppStructArray<>).MakeGenericType(elementType);
                else
                    appropriateArrayType = typeof(Il2CppReferenceArray<>).MakeGenericType(elementType);

                return myClassPointerToTypeMap[clazz] = appropriateArrayType;
            }

            var image = IL2CPP.il2cpp_class_get_image(clazz);
            var nativeToken = IL2CPP.il2cpp_class_get_type_token(clazz);

            if (!myAssemblyMap.TryGetValue(image, out var tuple))
            {
                LogSupport.Error($"Got unknown type: image {Marshal.PtrToStringAnsi(IL2CPP.il2cpp_image_get_name(image))} class {Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(clazz))}");
                return null;
            }

            if (tuple.Item1 == null) return null;

            var left = tuple.rangeStart;
            var right = tuple.rangeEnd;

            while (right - left > 1)
            {
                var mid = (left + right) / 2;
                var pointerAt = myValues[mid];
                if (pointerAt > nativeToken)
                    right = mid;
                else
                    left = mid;
            }

            // todo: handle primitive types (for boxes)

            if (myValues[left] != nativeToken)
            {
                LogSupport.Error($"Got unknown type: image {Marshal.PtrToStringAnsi(IL2CPP.il2cpp_image_get_name(image))} class {Marshal.PtrToStringAnsi(IL2CPP.il2cpp_class_get_name(clazz))}");
                return null;
            }

            var tokenOffset = left - tuple.rangeStart + tuple.tokensOffset;

            return myClassPointerToTypeMap[clazz] = tuple.Item1.ManifestModule.ResolveType(myValues[tokenOffset]);
        }

        public Type? LookupByObject(IntPtr nativeObject)
        {
            if (nativeObject == IntPtr.Zero)
                return null;

            var clazz = IL2CPP.il2cpp_object_get_class(nativeObject);

            return LookupByClass(clazz);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FileHeader
        {
            public int Magic;
            public int Version;
            public int NumAssemblies;
            public int DataOffset;

            // header is followed by (string il2CppAssemblyName, string managedAssemblyName, int tokenRangeStart, int tokenRangeEnd, int tokenOffset)[NumAssemblies]

            // data is int[]; parts within tokenRangeStart and tokenRangeEnd are native token values, sorted ascending per assembly. tokenOffset points to the range of managed token values corresponding to native tokens
        }
    }
}
