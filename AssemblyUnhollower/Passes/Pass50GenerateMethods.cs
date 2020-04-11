using System;
using System.Linq;
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
                        
                        foreach (var originalMethodParameter in originalMethod.Parameters)
                        {
                            var newParameter = new ParameterDefinition(originalMethodParameter.Name,
                                originalMethodParameter.Attributes & ~ParameterAttributes.HasFieldMarshal,
                                assemblyContext.RewriteTypeRef(originalMethodParameter.ParameterType));
                            newMethod.Parameters.Add(newParameter);
                        }

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

                        bodyBuilder.EmitLdcI4(originalMethod.Parameters.Count * IntPtr.Size);
                        bodyBuilder.Emit(OpCodes.Conv_U);
                        bodyBuilder.Emit(OpCodes.Localloc);
                        bodyBuilder.Emit(OpCodes.Stloc, argArray);

                        var argOffset = originalMethod.IsStatic ? 0 : 1;

                        for (var i = 0; i < newMethod.Parameters.Count; i++)
                        {
                            bodyBuilder.Emit(OpCodes.Ldloc, argArray);
                            bodyBuilder.EmitLdcI4(i * IntPtr.Size);
                            bodyBuilder.Emit(OpCodes.Add);

                            var newParam = newMethod.Parameters[i];

                            if (newParam.ParameterType.FullName == "System.String")
                            {
                                bodyBuilder.Emit(OpCodes.Ldarg, argOffset + i);
                                bodyBuilder.Emit(OpCodes.Call, imports.StringToNative);
                            } else if (newParam.ParameterType is GenericParameter)
                            {
                                GenerateGenericParameter(bodyBuilder, newMethod, newParam, imports, argOffset + i);
                            }
                            else if (newParam.ParameterType.IsValueType)
                            {
                                bodyBuilder.Emit(OpCodes.Ldarga, argOffset + i);
                            }
                            else
                            {
                                bodyBuilder.Emit(OpCodes.Ldarg, argOffset + i);
                                bodyBuilder.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
                            }

                            bodyBuilder.Emit(OpCodes.Stind_I);
                        }

                        if (originalMethod.IsVirtual || originalMethod.IsAbstract)
                        {
                            bodyBuilder.Emit(OpCodes.Ldarg_0);
                            bodyBuilder.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
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
                        {
                            if (originalMethod.DeclaringType.IsValueType)
                            {
                                bodyBuilder.Emit(OpCodes.Ldarg_0);
                            }
                            else
                            {
                                bodyBuilder.Emit(OpCodes.Ldarg_0);
                                bodyBuilder.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
                            }
                        }

                        bodyBuilder.Emit(OpCodes.Ldloc, argArray);
                        bodyBuilder.Emit(OpCodes.Ldloca, exceptionLocal);
                        bodyBuilder.Emit(OpCodes.Call, imports.RuntimeInvoke);
                        bodyBuilder.Emit(OpCodes.Stloc, resultVar);

                        bodyBuilder.Emit(OpCodes.Ldloc, exceptionLocal);
                        bodyBuilder.Emit(OpCodes.Call, imports.RaiseExceptionIfNecessary);

                        bodyBuilder.Emit(OpCodes.Ldloc, resultVar);
                        if (originalMethod.ReturnType.FullName == "System.Void")
                        {
                            bodyBuilder.Emit(OpCodes.Pop);
                        }
                        else if (originalMethod.ReturnType.FullName == "System.String")
                        {
                            bodyBuilder.Emit(OpCodes.Call, imports.StringFromNative);
                        }
                        else if (originalMethod.ReturnType.IsValueType)
                        {
                            bodyBuilder.Emit(OpCodes.Call, imports.ObjectUnbox);
                            bodyBuilder.Emit(OpCodes.Ldobj, newMethod.ReturnType);
                        } else if (originalMethod.ReturnType is GenericParameter)
                        {
                            GenerateGenericReturn(bodyBuilder, newMethod, assemblyContext.Imports);
                        }
                        else
                        {
                            var createRealObject = bodyBuilder.Create(OpCodes.Newobj,
                                new MethodReference(".ctor", imports.Void, newMethod.ReturnType)
                                    {Parameters = {new ParameterDefinition(imports.IntPtr)}, HasThis = true});

                            bodyBuilder.Emit(OpCodes.Dup);
                            bodyBuilder.Emit(OpCodes.Brtrue_S, createRealObject);
                            bodyBuilder.Emit(OpCodes.Pop);
                            bodyBuilder.Emit(OpCodes.Ldnull);
                            bodyBuilder.Emit(OpCodes.Ret);

                            bodyBuilder.Append(createRealObject);
                        }

                        bodyBuilder.Emit(OpCodes.Ret);
                    }
                }
            }
        }

        private static void GenerateGenericParameter(ILProcessor bodyBuilder, MethodDefinition method,
            ParameterDefinition parameter, AssemblyKnownImports imports, int argNumber)
        {
            bodyBuilder.Emit(OpCodes.Ldtoken, parameter.ParameterType);
            bodyBuilder.Emit(OpCodes.Call, method.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == nameof(Type.GetTypeFromHandle))));
            bodyBuilder.Emit(OpCodes.Call, method.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == typeof(Type).GetProperty(nameof(Type.IsValueType))!.GetMethod!.Name)));

            var finalNop = bodyBuilder.Create(OpCodes.Nop);
            var valueTypeNop = bodyBuilder.Create(OpCodes.Nop);
            
            bodyBuilder.Emit(OpCodes.Brtrue, valueTypeNop);
            
            bodyBuilder.Emit(OpCodes.Ldarg, argNumber);
            bodyBuilder.Emit(OpCodes.Box, parameter.ParameterType);
            bodyBuilder.Emit(OpCodes.Isinst, imports.Il2CppObjectBase);
            bodyBuilder.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointer);
            
            bodyBuilder.Emit(OpCodes.Br, finalNop);
            bodyBuilder.Append(valueTypeNop);
            
            bodyBuilder.Emit(OpCodes.Ldarga, argNumber);
            
            bodyBuilder.Append(finalNop);
        }

        private static void GenerateGenericReturn(ILProcessor bodyBuilder, MethodDefinition method, AssemblyKnownImports imports)
        {
            bodyBuilder.Emit(OpCodes.Ldtoken, method.ReturnType);
            bodyBuilder.Emit(OpCodes.Call, method.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == nameof(Type.GetTypeFromHandle))));
            bodyBuilder.Emit(OpCodes.Call, method.Module.ImportReference(imports.Type.Methods.Single(it => it.Name == typeof(Type).GetProperty(nameof(Type.IsValueType))!.GetMethod!.Name)));

            var finalNop = bodyBuilder.Create(OpCodes.Nop);
            var valueTypeNop = bodyBuilder.Create(OpCodes.Nop);
            
            bodyBuilder.Emit(OpCodes.Brtrue, valueTypeNop);
            
            var createRealObject = bodyBuilder.Create(OpCodes.Newobj,
                new MethodReference(".ctor", imports.Void, imports.Il2CppObjectBase)
                    {Parameters = {new ParameterDefinition(imports.IntPtr)}, HasThis = true});

            bodyBuilder.Emit(OpCodes.Dup);
            bodyBuilder.Emit(OpCodes.Brtrue_S, createRealObject);
            bodyBuilder.Emit(OpCodes.Pop);
            bodyBuilder.Emit(OpCodes.Ldnull);
            bodyBuilder.Emit(OpCodes.Br, finalNop);

            bodyBuilder.Append(createRealObject);
            bodyBuilder.Emit(OpCodes.Call, method.Module.ImportReference(new GenericInstanceMethod(imports.Il2CppObjectCast) { GenericArguments = { method.ReturnType }}));
            
            bodyBuilder.Emit(OpCodes.Br, finalNop);
            bodyBuilder.Append(valueTypeNop);
            
            bodyBuilder.Emit(OpCodes.Call, imports.ObjectUnbox);
            bodyBuilder.Emit(OpCodes.Ldobj, method.ReturnType);
            
            bodyBuilder.Append(finalNop);
        }
    }
}