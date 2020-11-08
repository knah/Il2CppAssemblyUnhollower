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

        private static void FindMetadataInitForMethod(RewriteGlobalContext context, long gameAssemblyBase)
        {
            var unityObjectCctor = context.Assemblies
                .Single(it => it.OriginalAssembly.Name.Name == "UnityEngine.CoreModule").GetTypeByName("UnityEngine.Object").OriginalType.Methods.Single(it => it.Name == ".cctor");

            MetadataInitForMethodFileOffset =
                (IntPtr) ((long) XrefScannerLowLevel.JumpTargets((IntPtr) (gameAssemblyBase + unityObjectCctor.ExtractOffset())).First());
            MetadataInitForMethodRva = (long) MetadataInitForMethodFileOffset - gameAssemblyBase - unityObjectCctor.ExtractOffset() + unityObjectCctor.ExtractRva();
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