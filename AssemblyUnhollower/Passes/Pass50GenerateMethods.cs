using System.Collections.Generic;
using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass50GenerateMethods
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var typeContext in assemblyContext.Types)
                {
                    foreach (var methodRewriteContext in typeContext.Methods)
                    {
                        var originalMethod = methodRewriteContext.OriginalMethod;
                        var newMethod = methodRewriteContext.NewMethod;
                        var imports = assemblyContext.Imports;

                        var bodyBuilder = newMethod.Body.GetILProcessor();
                        var exceptionLocal = new VariableDefinition(imports.IntPtr);
                        var argArray = new VariableDefinition(new PointerType(imports.IntPtr));
                        var resultVar = new VariableDefinition(imports.IntPtr);
                        var valueTypeLocal = new VariableDefinition(newMethod.ReturnType);
                        newMethod.Body.Variables.Add(exceptionLocal);
                        newMethod.Body.Variables.Add(argArray);
                        newMethod.Body.Variables.Add(resultVar);

                        if (valueTypeLocal.VariableType.FullName != "System.Void")
                            newMethod.Body.Variables.Add(valueTypeLocal);

                        if (!originalMethod.DeclaringType.IsValueType)
                        {
                            if (originalMethod.IsConstructor)
                            {
                                bodyBuilder.Emit(OpCodes.Ldarg_0);
                                bodyBuilder.Emit(OpCodes.Ldsfld, typeContext.ClassPointerFieldRef);
                                bodyBuilder.Emit(OpCodes.Call,imports.Il2CppNewObject);
                                bodyBuilder.Emit(OpCodes.Call,
                                    new MethodReference(".ctor", imports.Void, typeContext.SelfSubstitutedRef)
                                        {Parameters = {new ParameterDefinition(imports.IntPtr)}, HasThis = true});
                            }
                            else if (!originalMethod.IsStatic)
                            {
                                bodyBuilder.Emit(OpCodes.Ldarg_0);
                                bodyBuilder.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
                                bodyBuilder.Emit(OpCodes.Pop);
                            }
                        }

                        if (originalMethod.Parameters.Count == 0)
                        {
                            bodyBuilder.Emit(OpCodes.Ldc_I4_0);
                            bodyBuilder.Emit(OpCodes.Conv_U);
                        }
                        else
                        {
                            bodyBuilder.EmitLdcI4(originalMethod.Parameters.Count);
                            bodyBuilder.Emit(OpCodes.Conv_U);
                            bodyBuilder.Emit(OpCodes.Sizeof, imports.IntPtr);
                            bodyBuilder.Emit(OpCodes.Mul_Ovf_Un);
                            bodyBuilder.Emit(OpCodes.Localloc);
                        }
                        bodyBuilder.Emit(OpCodes.Stloc, argArray);

                        var argOffset = originalMethod.IsStatic ? 0 : 1;

                        var byRefParams = new List<(int, VariableDefinition)>();

                        for (var i = 0; i < newMethod.Parameters.Count; i++)
                        {
                            bodyBuilder.Emit(OpCodes.Ldloc, argArray);
                            if (i > 0) {
                                bodyBuilder.EmitLdcI4(i);
                                bodyBuilder.Emit(OpCodes.Conv_U);
                                bodyBuilder.Emit(OpCodes.Sizeof, imports.IntPtr);
                                bodyBuilder.Emit(OpCodes.Mul_Ovf_Un);
                                bodyBuilder.Emit(OpCodes.Add);
                            }

                            var newParam = newMethod.Parameters[i];
                            bodyBuilder.EmitObjectToPointer(originalMethod.Parameters[i].ParameterType, newParam.ParameterType, methodRewriteContext.DeclaringType, argOffset + i, false, true, true, out var refVar);
                            bodyBuilder.Emit(OpCodes.Stind_I);
                            
                            if(refVar != null)
                                byRefParams.Add((i, refVar));
                        }

                        if (originalMethod.IsVirtual && !originalMethod.DeclaringType.IsValueType || originalMethod.IsAbstract)
                        {
                            bodyBuilder.Emit(OpCodes.Ldarg_0);
                            bodyBuilder.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
                            if (methodRewriteContext.GenericInstantiationsStoreSelfSubstRef != null)
                            {
                                bodyBuilder.Emit(OpCodes.Ldsfld, new FieldReference("Pointer", imports.IntPtr, methodRewriteContext.GenericInstantiationsStoreSelfSubstMethodRef));
                            } else
                                bodyBuilder.Emit(OpCodes.Ldsfld, methodRewriteContext.NonGenericMethodInfoPointerField);
                            bodyBuilder.Emit(OpCodes.Call, imports.GetVirtualMethod);
                        }
                        else if (methodRewriteContext.GenericInstantiationsStoreSelfSubstRef != null)
                        {
                            bodyBuilder.Emit(OpCodes.Ldsfld, new FieldReference("Pointer", imports.IntPtr, methodRewriteContext.GenericInstantiationsStoreSelfSubstMethodRef));
                        } else
                            bodyBuilder.Emit(OpCodes.Ldsfld, methodRewriteContext.NonGenericMethodInfoPointerField);

                        if (originalMethod.IsStatic)
                            bodyBuilder.Emit(OpCodes.Ldc_I4_0);
                        else
                            bodyBuilder.EmitObjectToPointer(originalMethod.DeclaringType, newMethod.DeclaringType, typeContext, 0, true, false, true, out _);

                        bodyBuilder.Emit(OpCodes.Ldloc, argArray);
                        bodyBuilder.Emit(OpCodes.Ldloca, exceptionLocal);
                        bodyBuilder.Emit(OpCodes.Call, imports.RuntimeInvoke);
                        bodyBuilder.Emit(OpCodes.Stloc, resultVar);

                        bodyBuilder.Emit(OpCodes.Ldloc, exceptionLocal);
                        bodyBuilder.Emit(OpCodes.Call, imports.RaiseExceptionIfNecessary);
                        
                        foreach (var byRefParam in byRefParams)
                        {
                            var paramIndex = byRefParam.Item1;
                            var paramVariable = byRefParam.Item2;
                            bodyBuilder.EmitUpdateRef(newMethod.Parameters[paramIndex], paramIndex + argOffset, paramVariable, imports);
                        }

                        bodyBuilder.EmitPointerToObject(originalMethod.ReturnType, newMethod.ReturnType, typeContext, bodyBuilder.Create(OpCodes.Ldloc, resultVar), false, true);

                        bodyBuilder.Emit(OpCodes.Ret);
                    }
                }
            }
        }
    }
}