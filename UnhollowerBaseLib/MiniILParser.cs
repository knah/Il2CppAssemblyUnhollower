using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace UnhollowerBaseLib
{
    public static class MiniIlParser
    {
        private static readonly Dictionary<short, OpCode> OpCodesMap = new Dictionary<short, OpCode>();
        private static readonly HashSet<short> PrefixCodes = new HashSet<short>();

        static MiniIlParser()
        {
            foreach (var fieldInfo in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (fieldInfo.FieldType != typeof(OpCode)) continue;

                var fieldValue = (OpCode) fieldInfo.GetValue(null);
                OpCodesMap[fieldValue.Value] = fieldValue;
                if ((ushort) fieldValue.Value > byte.MaxValue) 
                    PrefixCodes.Add((byte) ((ushort) fieldValue.Value >> 8));
            }
        }

        public static IEnumerable<(OpCode, long)> Decode(byte[] ilBytes)
        {
            int index = 0;
            while (index < ilBytes.Length)
            {
                short currentOp = ilBytes[index++];
                if (PrefixCodes.Contains(currentOp)) 
                    currentOp = (short) ((ushort) (currentOp << 8) | ilBytes[index++]);

                if (!OpCodesMap.TryGetValue(currentOp, out var opCode))
                    throw new NotSupportedException($"Unknown opcode {currentOp} encountered");

                var argLength = GetOperandSize(opCode);
                
                switch (argLength)
                {
                    case 0:
                        yield return (opCode, 0);
                        break;
                    case 1:
                        yield return (opCode, ilBytes[index]);
                        break;
                    case 2:
                        yield return (opCode, BitConverter.ToInt16(ilBytes, index));
                        break;
                    case 4:
                        yield return (opCode, BitConverter.ToInt32(ilBytes, index));
                        break;
                    case 8:
                        yield return (opCode, BitConverter.ToInt64(ilBytes, index));
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported opcode argument length {argLength}");
                }
                
                index += argLength;
            }
        }

        private static int GetOperandSize(OpCode opCode)
        {
            switch (opCode.OperandType)
            {
                case OperandType.InlineField:
                case OperandType.InlineBrTarget:
                case OperandType.InlineI:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineString:
                case OperandType.InlineSwitch:
                case OperandType.InlineTok:
                case OperandType.InlineType:
                case OperandType.ShortInlineR:
                    return 4;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    return 8;
                case OperandType.InlineNone:
                    return 0;
                case OperandType.InlineVar:
                    return 2;
                case OperandType.ShortInlineBrTarget:
                case OperandType.ShortInlineVar:
                case OperandType.ShortInlineI:
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}