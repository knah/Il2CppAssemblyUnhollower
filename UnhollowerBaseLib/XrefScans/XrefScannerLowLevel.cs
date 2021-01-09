using System;
using System.Collections.Generic;
using Iced.Intel;
using Decoder = Iced.Intel.Decoder;

namespace UnhollowerRuntimeLib.XrefScans
{
    public static class XrefScannerLowLevel
    {
        public static IEnumerable<IntPtr> JumpTargets(IntPtr codeStart)
        {
            return JumpTargetsImpl(XrefScanner.DecoderForAddress(codeStart));
        }

        private static IEnumerable<IntPtr> JumpTargetsImpl(Decoder myDecoder)
        {
            while (true)
            {
                myDecoder.Decode(out var instruction);
                if (myDecoder.LastError == DecoderError.NoMoreBytes) yield break;
                if (instruction.FlowControl == FlowControl.Return)
                    yield break;

                if (instruction.FlowControl == FlowControl.UnconditionalBranch || instruction.FlowControl == FlowControl.Call)
                {
                    yield return (IntPtr) ExtractTargetAddress(in instruction);
                    if(instruction.FlowControl == FlowControl.UnconditionalBranch) yield break;
                }
            }
        }

        public static IEnumerable<IntPtr> CallAndIndirectTargets(IntPtr pointer) => CallAndIndirectTargetsImpl(XrefScanner.DecoderForAddress(pointer, 1024 * 1024));

        private static IEnumerable<IntPtr> CallAndIndirectTargetsImpl(Decoder decoder)
        {
            while (true)
            {
                decoder.Decode(out var instruction);
                if (decoder.LastError == DecoderError.NoMoreBytes) yield break;

                if (instruction.FlowControl == FlowControl.Return)
                    yield break;

                if (instruction.Mnemonic == Mnemonic.Int || instruction.Mnemonic == Mnemonic.Int1)
                    yield break;

                if (instruction.Mnemonic == Mnemonic.Call || instruction.Mnemonic == Mnemonic.Jmp)
                {
                    var targetAddress = XrefScanner.ExtractTargetAddress(instruction);
                    if (targetAddress != 0)
                        yield return (IntPtr) targetAddress;
                    continue;
                }

                if (instruction.Mnemonic == Mnemonic.Lea)
                {
                    if (instruction.MemoryBase == Register.RIP)
                    {
                        var targetAddress = instruction.IPRelativeMemoryAddress;
                        if (targetAddress != 0)
                            yield return (IntPtr) targetAddress;
                    }
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
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}