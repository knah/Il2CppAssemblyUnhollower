using System;
using Iced.Intel;

namespace UnhollowerRuntimeLib.XrefScans
{
    internal static class XrefScanUtilFinder
    {
        public static IntPtr FindLastRcxReadAddressBeforeCallTo(IntPtr codeStart, IntPtr callTarget)
        {
            var decoder = XrefScanner.DecoderForAddress(codeStart);
            IntPtr lastRcxRead = IntPtr.Zero;
            
            while (true)
            {
                decoder.Decode(out var instruction);
                if (decoder.LastError == DecoderError.NoMoreBytes) return IntPtr.Zero;

                if (instruction.FlowControl == FlowControl.Return)
                    return IntPtr.Zero;

                if (instruction.FlowControl == FlowControl.UnconditionalBranch)
                    continue;

                if (instruction.Mnemonic == Mnemonic.Int || instruction.Mnemonic == Mnemonic.Int1)
                    return IntPtr.Zero;

                if (instruction.Mnemonic == Mnemonic.Call)
                {
                    var target = ExtractTargetAddress(instruction);
                    if ((IntPtr) target == callTarget)
                        return lastRcxRead;
                }

                if (instruction.Mnemonic == Mnemonic.Mov)
                {
                    if (instruction.Op0Kind == OpKind.Register && instruction.Op0Register == Register.ECX && instruction.Op1Kind == OpKind.Memory && instruction.IsIPRelativeMemoryOperand)
                    {
                        var movTarget = (IntPtr) instruction.IPRelativeMemoryAddress;
                        if (instruction.MemorySize != MemorySize.UInt32 && instruction.MemorySize != MemorySize.Int32) 
                            continue;
                        
                        lastRcxRead = movTarget;
                    }
                }
            }
        }
        
        public static IntPtr FindByteWriteTargetRightAfterCallTo(IntPtr codeStart, IntPtr callTarget)
        {
            var decoder = XrefScanner.DecoderForAddress(codeStart);
            var seenCall = false;
            
            while (true)
            {
                decoder.Decode(out var instruction);
                if (decoder.LastError == DecoderError.NoMoreBytes) return IntPtr.Zero;

                if (instruction.FlowControl == FlowControl.Return)
                    return IntPtr.Zero;

                if (instruction.FlowControl == FlowControl.UnconditionalBranch)
                    continue;

                if (instruction.Mnemonic == Mnemonic.Int || instruction.Mnemonic == Mnemonic.Int1)
                    return IntPtr.Zero;

                if (instruction.Mnemonic == Mnemonic.Call)
                {
                    var target = ExtractTargetAddress(instruction);
                    if ((IntPtr) target == callTarget)
                        seenCall = true;
                }

                if (instruction.Mnemonic == Mnemonic.Mov && seenCall)
                {
                    if (instruction.Op0Kind == OpKind.Memory && (instruction.MemorySize == MemorySize.Int8 || instruction.MemorySize == MemorySize.UInt8))
                        return (IntPtr) instruction.IPRelativeMemoryAddress;
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