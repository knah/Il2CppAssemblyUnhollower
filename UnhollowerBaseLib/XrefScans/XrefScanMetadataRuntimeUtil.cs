using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnhollowerBaseLib;
using AppDomain = Il2CppSystem.AppDomain;
using BindingFlags = Il2CppSystem.Reflection.BindingFlags;

namespace UnhollowerRuntimeLib.XrefScans
{
    internal static class XrefScanMetadataRuntimeUtil
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void InitMetadataForMethod(int metadataUsageToken);

        private static InitMetadataForMethod ourMetadataInitForMethodDelegate;
        private static IntPtr ourMetadataInitForMethodPointer;

        private static unsafe void FindMetadataInitForMethod()
        {
            var unityObjectCctor = AppDomain.CurrentDomain.GetAssemblies()
                .Single(it => it.GetSimpleName() == "UnityEngine.CoreModule").GetType("UnityEngine.Object")
                .GetConstructors(BindingFlags.Static | BindingFlags.NonPublic).Single();
            var nativeMethodInfo = IL2CPP.il2cpp_method_get_from_reflection(unityObjectCctor.Pointer);
            ourMetadataInitForMethodPointer = XrefScannerLowLevel.JumpTargets(*(IntPtr*) nativeMethodInfo).First();
            ourMetadataInitForMethodDelegate = Marshal.GetDelegateForFunctionPointer<InitMetadataForMethod>(ourMetadataInitForMethodPointer);
        }

        internal static unsafe bool CallMetadataInitForMethod(MethodBase method)
        {
            if (ourMetadataInitForMethodPointer == IntPtr.Zero)
                FindMetadataInitForMethod();

            var nativeMethodInfoObject = UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method)?.GetValue(null);
            if (nativeMethodInfoObject == null) return false;
            var nativeMethodInfo = (IntPtr) nativeMethodInfoObject;
            var codeStart = *(IntPtr*) nativeMethodInfo;
            var firstCall = XrefScannerLowLevel.JumpTargets(codeStart).FirstOrDefault();
            if (firstCall != ourMetadataInitForMethodPointer || firstCall == IntPtr.Zero) return false;

            var tokenPointer = XrefScanUtilFinder.FindLastRcxReadAddressBeforeCallTo(codeStart, ourMetadataInitForMethodPointer);
            var initFlagPointer = XrefScanUtilFinder.FindByteWriteTargetRightAfterCallTo(codeStart, ourMetadataInitForMethodPointer);

            if (tokenPointer == IntPtr.Zero || initFlagPointer == IntPtr.Zero) return false;

            if (Marshal.ReadByte(initFlagPointer) == 0)
            {
                ourMetadataInitForMethodDelegate(Marshal.ReadInt32(tokenPointer));
                Marshal.WriteByte(initFlagPointer, 1);
            }

            return true;
        } 
    }
}