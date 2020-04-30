using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Iced.Intel;
using UnhollowerBaseLib;
using Decoder = Iced.Intel.Decoder;

namespace UnhollowerRuntimeLib
{
    public class XrefScanner : IDisposable
    {
        private readonly Decoder myDecoder;
        private readonly UnmanagedMemoryStream myUnmanagedMemoryStream;

        public unsafe XrefScanner(IntPtr codeStart, int lengthLimit = 1000)
        {
            if (codeStart == IntPtr.Zero) throw new NullReferenceException(nameof(codeStart));

            myUnmanagedMemoryStream = new UnmanagedMemoryStream((byte*) codeStart, lengthLimit, lengthLimit, FileAccess.Read);
            var codeReader = new StreamCodeReader(myUnmanagedMemoryStream);
            myDecoder = Decoder.Create(IntPtr.Size * 8, codeReader);
            myDecoder.IP = (ulong) codeStart;
        }

        public IEnumerable<IntPtr> JumpTargets()
        {
            var formatter = new IntelFormatter();
            var builder = new StringBuilder();
            while (true)
            {
                myDecoder.Decode(out var instruction);
                builder.Clear();
                formatter.Format(in instruction, new StringOutput(builder));
                LogSupport.Trace($"Decoded instruction: {builder}");
                if (myDecoder.InvalidNoMoreBytes) yield break;
                if (instruction.FlowControl == FlowControl.Return)
                    yield break;

                if (instruction.FlowControl == FlowControl.UnconditionalBranch || instruction.FlowControl == FlowControl.Call)
                {
                    yield return (IntPtr) ExtractTargetAddress(in instruction);
                    if(instruction.FlowControl == FlowControl.UnconditionalBranch) yield break;
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

        public void Dispose()
        {
            myUnmanagedMemoryStream?.Dispose();
        }
    }
}