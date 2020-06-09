using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Iced.Intel;
using UnhollowerBaseLib;
using Type = Il2CppSystem.Type;

namespace UnhollowerRuntimeLib.XrefScans
{
    public static class XrefScanner
    {
        public static unsafe IEnumerable<XrefInstance> XrefScan(MethodBase methodBase)
        {
            var fieldValue = UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(methodBase)?.GetValue(null);
            if (fieldValue == null) return Enumerable.Empty<XrefInstance>();
            
            XrefScanMetadataUtil.CallMetadataInitForMethod(methodBase);

            return XrefScanImpl(DecoderForAddress(*(IntPtr*) (IntPtr) fieldValue));
        }

        internal static unsafe Decoder DecoderForAddress(IntPtr codeStart, int lengthLimit = 1000)
        {
            if (codeStart == IntPtr.Zero) throw new NullReferenceException(nameof(codeStart));
            
            var stream = new UnmanagedMemoryStream((byte*) codeStart, lengthLimit, lengthLimit, FileAccess.Read);
            var codeReader = new StreamCodeReader(stream);
            var decoder = Decoder.Create(IntPtr.Size * 8, codeReader);
            decoder.IP = (ulong) codeStart;

            return decoder;
        }

        private static IEnumerable<XrefInstance> XrefScanImpl(Decoder decoder)
        {
            while (true)
            {
                decoder.Decode(out var instruction);
                if (decoder.InvalidNoMoreBytes) yield break;

                if (instruction.FlowControl == FlowControl.Return)
                    yield break;

                if (instruction.Mnemonic == Mnemonic.Int || instruction.Mnemonic == Mnemonic.Int1)
                    yield break;

                if (instruction.Mnemonic == Mnemonic.Call || instruction.Mnemonic == Mnemonic.Jmp)
                {
                    var targetAddress = ExtractTargetAddress(instruction);
                    if (targetAddress != 0)
                        yield return new XrefInstance(XrefType.Method, (IntPtr) targetAddress);
                    continue;
                }
                
                if (instruction.FlowControl == FlowControl.UnconditionalBranch)
                    continue;

                if (instruction.Mnemonic == Mnemonic.Mov)
                {
                    XrefInstance? result = null;
                    try
                    {
                        if (instruction.Op1Kind == OpKind.Memory && instruction.IsIPRelativeMemoryOperand)
                        {
                            var movTarget = (IntPtr) instruction.IPRelativeMemoryAddress;
                            if (instruction.MemorySize != MemorySize.UInt64) 
                                continue;
                            
                            var valueAtMov = (IntPtr) Marshal.ReadInt64(movTarget);
                            if (valueAtMov != IntPtr.Zero)
                            {
                                var targetClass = (IntPtr) Marshal.ReadInt64(valueAtMov);
                                if (targetClass == Il2CppClassPointerStore<string>.NativeClassPtr || targetClass == Il2CppClassPointerStore<Type>.NativeClassPtr)
                                {
                                    result = new XrefInstance(XrefType.Global, movTarget);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogSupport.Error(ex.ToString());
                    }

                    if (result != null)
                        yield return result.Value;
                }
            }
        }

        private static ulong ExtractTargetAddress(in Instruction instruction)
        {
            switch (instruction.Op0Kind)
            {
                case OpKind.NearBranch16:
                    return instruction.NearBranch16;
                case OpKind.NearBranch32:
                    return instruction.NearBranch32;
                case OpKind.NearBranch64:
                    return instruction.NearBranch64;
                case OpKind.FarBranch16:
                    return instruction.FarBranch16;
                case OpKind.FarBranch32:
                    return instruction.FarBranch32;
                default:
                    return 0;
            }
        }
    }
}