using System.Linq;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Passes;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AssemblyUnhollower.Utils
{
    public static class UnstripTranslator
    {
        public static bool TranslateMethod(MethodDefinition original, MethodDefinition target, TypeRewriteContext typeRewriteContext, AssemblyKnownImports imports)
        {
            if (!original.HasBody) return true;
            
            var globalContext = typeRewriteContext.AssemblyContext.GlobalContext;
            foreach (var variableDefinition in original.Body.Variables)
            {
                var variableType = Pass80UnstripMethods.ResolveTypeInNewAssemblies(globalContext, variableDefinition.VariableType, imports);
                if (variableType == null) return false;
                target.Body.Variables.Add(new VariableDefinition(variableType));
            }
            
            var targetBuilder = target.Body.GetILProcessor();
            foreach (var bodyInstruction in original.Body.Instructions)
            {
                if (bodyInstruction.OpCode.OperandType == OperandType.InlineField)
                {
                    var fieldArg = (FieldReference) bodyInstruction.Operand;
                    var fieldDeclarer = Pass80UnstripMethods.ResolveTypeInNewAssembliesRaw(globalContext, fieldArg.DeclaringType, imports);
                    if (fieldDeclarer == null) return false;
                    var newField = fieldDeclarer.Resolve().Fields.SingleOrDefault(it => it.Name == fieldArg.Name);
                    if (newField != null)
                    {
                        targetBuilder.Emit(bodyInstruction.OpCode, imports.Module.ImportReference(newField));
                    }
                    else
                    {
                        if (bodyInstruction.OpCode == OpCodes.Ldfld || bodyInstruction.OpCode == OpCodes.Ldsfld)
                        {
                            var getterMethod = fieldDeclarer.Resolve().Properties.SingleOrDefault(it => it.Name == fieldArg.Name)?.GetMethod;
                            if (getterMethod == null) return false;

                            targetBuilder.Emit(OpCodes.Call, imports.Module.ImportReference(getterMethod));
                        } else if (bodyInstruction.OpCode == OpCodes.Stfld || bodyInstruction.OpCode == OpCodes.Stsfld)
                        {
                            var setterMethod = fieldDeclarer.Resolve().Properties.SingleOrDefault(it => it.Name == fieldArg.Name)?.SetMethod;
                            if (setterMethod == null) return false;

                            targetBuilder.Emit(OpCodes.Call, imports.Module.ImportReference(setterMethod));
                        }
                        else
                            return false;
                    }
                } else if (bodyInstruction.OpCode.OperandType == OperandType.InlineMethod)
                {
                    var methodArg = (MethodReference) bodyInstruction.Operand;
                    var methodDeclarer = Pass80UnstripMethods.ResolveTypeInNewAssemblies(globalContext, methodArg.DeclaringType, imports);
                    if (methodDeclarer == null) return false; // todo: generic methods

                    var newReturnType = Pass80UnstripMethods.ResolveTypeInNewAssemblies(globalContext, methodArg.ReturnType, imports);
                    if (newReturnType == null) return false;
                    
                    var newMethod = new MethodReference(methodArg.Name, newReturnType, methodDeclarer);
                    newMethod.HasThis = methodArg.HasThis;
                    foreach (var methodArgParameter in methodArg.Parameters)
                    {
                        var newParamType = Pass80UnstripMethods.ResolveTypeInNewAssemblies(globalContext, methodArgParameter.ParameterType, imports);
                        if (newParamType == null) return false;
                        
                        var newParam = new ParameterDefinition(methodArgParameter.Name, methodArgParameter.Attributes, newParamType);
                        newMethod.Parameters.Add(newParam);
                    }
                    
                    targetBuilder.Emit(bodyInstruction.OpCode, imports.Module.ImportReference(newMethod));
                } else if (bodyInstruction.OpCode.OperandType == OperandType.InlineType)
                {
                    var typeArg = (TypeReference) bodyInstruction.Operand;
                    var newType = Pass80UnstripMethods.ResolveTypeInNewAssemblies(globalContext, typeArg, imports);
                    if (newType == null) return false;
                    
                    targetBuilder.Emit(bodyInstruction.OpCode, newType); // todo: special case for castclass, isclass?
                } else if (bodyInstruction.OpCode.OperandType == OperandType.InlineSig)
                {
                    // todo: rewrite sig if this ever happens in unity types
                    return false;
                } else if (bodyInstruction.OpCode.OperandType == OperandType.InlineTok)
                {
                    var targetTok = (TypeReference) bodyInstruction.Operand;
                    if (targetTok is GenericParameter genericParam)
                    {
                        if (genericParam.Owner is TypeReference paramOwner)
                        {
                            var newTypeOwner = Pass80UnstripMethods.ResolveTypeInNewAssemblies(globalContext, paramOwner, imports);
                            if (newTypeOwner == null) return false;
                            targetTok = newTypeOwner.GenericParameters.Single(it => it.Name == targetTok.Name);
                        } else
                            targetTok = target.GenericParameters.Single(it => it.Name == targetTok.Name);
                    }
                    else
                    {
                        targetTok = Pass80UnstripMethods.ResolveTypeInNewAssemblies(globalContext, targetTok, imports);
                        if (targetTok == null) return false;
                    }
                    
                    targetBuilder.Emit(OpCodes.Call, imports.Module.ImportReference(new GenericInstanceMethod(imports.LdTokUnstrippedImpl) { GenericArguments = { targetTok }}));
                }
                else
                {
                    targetBuilder.Append(bodyInstruction);
                }
            }

            return true;
        }

        public static void ReplaceBodyWithException(MethodDefinition newMethod, AssemblyKnownImports imports)
        {
            newMethod.Body.Variables.Clear();
            newMethod.Body.Instructions.Clear();
            var processor = newMethod.Body.GetILProcessor();
            
            processor.Emit(OpCodes.Ldstr, "Method unstripping failed");
            processor.Emit(OpCodes.Newobj, imports.NotSupportedExceptionCtor);
            processor.Emit(OpCodes.Throw);
            processor.Emit(OpCodes.Ret);
        }
    }
}