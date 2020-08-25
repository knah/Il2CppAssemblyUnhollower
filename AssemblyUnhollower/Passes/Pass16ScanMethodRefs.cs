using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using AssemblyUnhollower.Contexts;
using UnhollowerRuntimeLib.XrefScans;

namespace AssemblyUnhollower.Passes
{
    public static class Pass16ScanMethodRefs
    {
        public static readonly HashSet<long> NonDeadMethods = new HashSet<long>();
        public static Dictionary<long, List<long>> MapOfCallers;

        public static void DoPass(RewriteGlobalContext context, UnhollowerOptions options)
        {
            if (string.IsNullOrEmpty(options.GameAssemblyPath))
            {
                Pass15GenerateMemberContexts.HasObfuscatedMethods = false;
                return;
            }
            if (!Pass15GenerateMemberContexts.HasObfuscatedMethods) return;

            var methodToCallersMap = new Dictionary<long, List<long>>();
            var methodToCalleesMap = new Dictionary<long, List<long>>();

            using var mappedFile = MemoryMappedFile.CreateFromFile(options.GameAssemblyPath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using var accessor = mappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            IntPtr gameAssemblyPtr;

            unsafe
            {
                byte* fileStartPtr = null;
                accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref fileStartPtr);
                gameAssemblyPtr = (IntPtr) fileStartPtr;
            }

            // Scan xrefs
            foreach (var assemblyRewriteContext in context.Assemblies)
            foreach (var typeRewriteContext in assemblyRewriteContext.Types)
            foreach (var originalTypeMethod in typeRewriteContext.Methods)
            {
                var address = originalTypeMethod.FileOffset;
                if (address == 0) continue;

                foreach (var callTarget in XrefScannerLowLevel.CallAndIndirectTargets(IntPtr.Add(gameAssemblyPtr, (int) address)))
                {
                    var targetRelative = (long) callTarget - (long) gameAssemblyPtr;
                    methodToCallersMap.GetOrCreate(targetRelative, _ => new List<long>()).Add(address);
                    methodToCalleesMap.GetOrCreate(address, _ => new List<long>()).Add(targetRelative);
                }
            }

            MapOfCallers = methodToCallersMap;

            void MarkMethodAlive(long address)
            {
                if (!NonDeadMethods.Add(address)) return;
                if (!methodToCalleesMap.TryGetValue(address, out var calleeList)) return;
                
                foreach (var callee in calleeList) 
                    MarkMethodAlive(callee);
            }
            
            // Now decided which of them are possible dead code
            foreach (var assemblyRewriteContext in context.Assemblies)
            foreach (var typeRewriteContext in assemblyRewriteContext.Types)
            foreach (var methodRewriteContext in typeRewriteContext.Methods)
            {
                if (methodRewriteContext.FileOffset == 0) continue;
                
                var originalMethod = methodRewriteContext.OriginalMethod;
                if (!originalMethod.Name.IsObfuscated() || originalMethod.IsVirtual)
                    MarkMethodAlive(methodRewriteContext.FileOffset);
            }
        }
    }
}