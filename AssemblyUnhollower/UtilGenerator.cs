using Mono.Cecil;
using Mono.Cecil.Cil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower
{
    public static class UtilGenerator
    {
        private static readonly OpCode[] I4Constants = {
            OpCodes.Ldc_I4_M1,
            OpCodes.Ldc_I4_0,
            OpCodes.Ldc_I4_1,
            OpCodes.Ldc_I4_2,
            OpCodes.Ldc_I4_3,
            OpCodes.Ldc_I4_4,
            OpCodes.Ldc_I4_5,
            OpCodes.Ldc_I4_6,
            OpCodes.Ldc_I4_7,
            OpCodes.Ldc_I4_8,
        };

        public static void EmitLdcI4(this ILProcessor body, int constant)
        {
            if(constant >= -1 && constant <= 8)
                body.Emit(I4Constants[constant + 1]);
            else if(constant >= byte.MinValue && constant <= byte.MaxValue)
                body.Emit(OpCodes.Ldc_I4_S, (sbyte) constant);
            else
                body.Emit(OpCodes.Ldc_I4, constant);
        }

        public static void GenerateBoxMethod(TypeDefinition targetType, FieldReference classHandle, TypeReference il2CppObjectTypeDef)
        {
            var method = new MethodDefinition("BoxIl2CppObject", MethodAttributes.Public | MethodAttributes.HideBySig, targetType.Module.ImportReference(il2CppObjectTypeDef));
            targetType.Methods.Add(method);

            var methodBody = method.Body.GetILProcessor();
            methodBody.Emit(OpCodes.Ldsfld, classHandle);
            methodBody.Emit(OpCodes.Ldarg_0);
            methodBody.Emit(OpCodes.Call, targetType.Module.ImportReference(typeof(IL2CPP).GetMethod("il2cpp_value_box")));

            methodBody.Emit(OpCodes.Newobj, new MethodReference(".ctor", targetType.Module.ImportReference(TargetTypeSystemHandler.Void), il2CppObjectTypeDef) { Parameters = { new ParameterDefinition(targetType.Module.ImportReference(TargetTypeSystemHandler.IntPtr))}, HasThis = true});
            
            methodBody.Emit(OpCodes.Ret);
        }
    }
}