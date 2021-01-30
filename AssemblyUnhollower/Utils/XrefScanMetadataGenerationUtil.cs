using System;
using System.Linq;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;

namespace UnhollowerRuntimeLib.XrefScans
{
    internal static class XrefScanMetadataGenerationUtil
    {
        internal static long MetadataInitForMethodRva;
        internal static IntPtr MetadataInitForMethodFileOffset;

        private static readonly (string Assembly, string Type, string Method)[] MetadataInitCandidates = {
            ("UnityEngine.CoreModule", "UnityEngine.Object", ".cctor"),
            ("mscorlib", "System.Exception", "get_Message"),
            ("mscorlib", "System.IntPtr", "Equals")
        };

        private static void FindMetadataInitForMethod(RewriteGlobalContext context, long gameAssemblyBase)
        {
            foreach (var metadataInitCandidate in MetadataInitCandidates)
            {
                var assembly = context.Assemblies.FirstOrDefault(it => it.OriginalAssembly.Name.Name == metadataInitCandidate.Assembly);
                var unityObjectCctor = assembly?.TryGetTypeByName(metadataInitCandidate.Type)?.OriginalType.Methods.FirstOrDefault(it => it.Name == metadataInitCandidate.Method);
                
                if(unityObjectCctor == null) continue;
                
                MetadataInitForMethodFileOffset =
                    (IntPtr) ((long) XrefScannerLowLevel.JumpTargets((IntPtr) (gameAssemblyBase + unityObjectCctor.ExtractOffset())).First());
                MetadataInitForMethodRva = (long) MetadataInitForMethodFileOffset - gameAssemblyBase - unityObjectCctor.ExtractOffset() + unityObjectCctor.ExtractRva();

                return;
            }

            throw new ApplicationException("Unable to find a method with metadata init reference");
        }

        internal static (long FlagRva, long TokenRva) FindMetadataInitForMethod(MethodRewriteContext method, long gameAssemblyBase)
        {
            if (MetadataInitForMethodRva == 0)
                FindMetadataInitForMethod(method.DeclaringType.AssemblyContext.GlobalContext, gameAssemblyBase);
            
            var codeStart = (IntPtr) (gameAssemblyBase + method.FileOffset);
            var firstCall = XrefScannerLowLevel.JumpTargets(codeStart).FirstOrDefault();
            if (firstCall != MetadataInitForMethodFileOffset || firstCall == IntPtr.Zero) return (0, 0);

            var tokenPointer = XrefScanUtilFinder.FindLastRcxReadAddressBeforeCallTo(codeStart, MetadataInitForMethodFileOffset);
            var initFlagPointer = XrefScanUtilFinder.FindByteWriteTargetRightAfterCallTo(codeStart, MetadataInitForMethodFileOffset);

            if (tokenPointer == IntPtr.Zero || initFlagPointer == IntPtr.Zero) return (0, 0);

            return ((long) initFlagPointer - gameAssemblyBase - method.FileOffset + method.Rva, (long) tokenPointer - gameAssemblyBase - method.FileOffset + method.Rva);
        } 
    }
}