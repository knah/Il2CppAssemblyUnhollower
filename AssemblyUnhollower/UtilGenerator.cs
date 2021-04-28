using System.Diagnostics;
using AssemblyUnhollower.Contexts;
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

        public static void EmitMethodThisToPointer(this ILProcessor body, TypeReference originalType, TypeReference newType, TypeRewriteContext enclosingType, int argumentIndex)
        {
            // input stack: not used
            // output stack: IntPtr to either Il2CppObject or IL2CPP value type

            if (originalType is GenericParameter)
            {
                Debug.Fail("Can't emit generic object-to-pointers");
                return;
            }

            var imports = enclosingType.AssemblyContext.Imports;
            if (originalType is ByReferenceType)
            {
                Debug.Fail("Can't emit byref object-to-pointers");
            }
            else if (originalType.IsValueType)
            {
                if (newType.IsValueType)
                {
                    body.Emit(OpCodes.Ldarg_0);
                }
                else
                {
                    body.Emit(OpCodes.Ldarg_0);
                    body.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
                    body.Emit(OpCodes.Call, imports.ObjectUnbox);
                }
            }
            else if (newType.FullName == "System.String")
            {
                Debug.Fail("Can't emit string object-to-pointers");
            } 
            else 
            {
                body.Emit(OpCodes.Ldarg_0);
                body.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
            }
        }

        public static void GenerateExitMethodCallFinallyBlock(this MethodDefinition newMethod, AssemblyKnownImports imports)
        {
            if (newMethod.ReturnType.FullName == "System.Void")
                GenerateExitMethodCallFinallyBlockVoidImpl(newMethod, imports);
            else
                GenerateExitMethodCallFinallyBlockValueImpl(newMethod, imports);
        }

        private static void GenerateExitMethodCallFinallyBlockVoidImpl(this MethodDefinition newMethod,
            AssemblyKnownImports imports)
        {
            var bodyBuilder = newMethod.Body.GetILProcessor();
            
            var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = newMethod.Body.Instructions[1],
            };

            var ret = bodyBuilder.Create(OpCodes.Ret);
            
            bodyBuilder.Emit(OpCodes.Leave, ret);

            var finallyInstr = bodyBuilder.Create(OpCodes.Call, imports.ScratchSpaceLeave);
            var endFinally = bodyBuilder.Create(OpCodes.Endfinally);
            bodyBuilder.Append(finallyInstr);
            bodyBuilder.Append(endFinally);

            exceptionHandler.TryEnd = finallyInstr;
                            
            bodyBuilder.Append(ret);

            exceptionHandler.HandlerStart = finallyInstr;
            exceptionHandler.HandlerEnd = ret;
                            
            newMethod.Body.ExceptionHandlers.Add(exceptionHandler);
        }

        private static void GenerateExitMethodCallFinallyBlockValueImpl(this MethodDefinition newMethod,
            AssemblyKnownImports imports)
        {
            var bodyBuilder = newMethod.Body.GetILProcessor();
            
            var exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = newMethod.Body.Instructions[1],
            };
                            
            var resultLocal = new VariableDefinition(newMethod.ReturnType);
                            
            newMethod.Body.Variables.Add(resultLocal);

            var ldResult = bodyBuilder.Create(OpCodes.Ldloc, resultLocal);
                            
            bodyBuilder.Emit(OpCodes.Stloc, resultLocal);
            bodyBuilder.Emit(OpCodes.Leave, ldResult);

            var finallyInstr = bodyBuilder.Create(OpCodes.Call, imports.ScratchSpaceLeave);
            var endFinally = bodyBuilder.Create(OpCodes.Endfinally);
            bodyBuilder.Append(finallyInstr);
            bodyBuilder.Append(endFinally);

            exceptionHandler.TryEnd = finallyInstr;
                            
            bodyBuilder.Append(ldResult);
            bodyBuilder.Emit(OpCodes.Ret);

            exceptionHandler.HandlerStart = finallyInstr;
            exceptionHandler.HandlerEnd = ldResult;
                            
            newMethod.Body.ExceptionHandlers.Add(exceptionHandler);
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