using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower.Utils
{
    public static class UnstripGenerator
    {
        public static TypeDefinition CreateDelegateTypeForICallMethod(MethodDefinition unityMethod, MethodDefinition convertedMethod, AssemblyKnownImports imports)
        {
            var delegateType = new TypeDefinition("", unityMethod.Name + "Delegate", TypeAttributes.Sealed | TypeAttributes.NestedPrivate, imports.MulticastDelegate);
            
            var constructor = new MethodDefinition(".ctor", MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Public, imports.Void);
            constructor.Parameters.Add(new ParameterDefinition(imports.Object));
            constructor.Parameters.Add(new ParameterDefinition(imports.IntPtr));
            constructor.ImplAttributes = MethodImplAttributes.CodeTypeMask;
            delegateType.Methods.Add(constructor);
            
            var invokeMethod = new MethodDefinition("Invoke", MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Public, imports.Void); // todo
            invokeMethod.ImplAttributes = MethodImplAttributes.CodeTypeMask;
            delegateType.Methods.Add(invokeMethod);

            invokeMethod.ReturnType = convertedMethod.ReturnType.IsValueType ? convertedMethod.ReturnType : imports.IntPtr;
            if (convertedMethod.HasThis)
                invokeMethod.Parameters.Add(new ParameterDefinition("@this", ParameterAttributes.None, imports.IntPtr));
            foreach (var convertedParameter in convertedMethod.Parameters)
                invokeMethod.Parameters.Add(new ParameterDefinition(convertedParameter.Name,
                    convertedParameter.Attributes,
                    convertedParameter.ParameterType.IsValueType ? convertedParameter.ParameterType : imports.IntPtr));

            return delegateType;
        }

        public static void GenerateInvokerMethodBody(MethodDefinition newMethod, FieldDefinition delegateField, TypeDefinition delegateType, TypeRewriteContext enclosingType, AssemblyKnownImports imports)
        {
            var body = newMethod.Body.GetILProcessor();
            
            // todo: support non-blittable structs here
            
            var needScratchSpace =
                newMethod.Parameters.Any(it => it.ParameterType.MayRequireScratchSpace()) ||
                newMethod.ReturnType.MayRequireScratchSpace();

            if (needScratchSpace) 
                body.Emit(OpCodes.Call, imports.ScratchSpaceEnter);
            
            body.Emit(OpCodes.Ldsfld, delegateField);
            if (newMethod.HasThis)
            {
                body.Emit(OpCodes.Ldarg_0);
                body.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
            }

            var argOffset = newMethod.HasThis ? 1 : 0;
            var byRefParams = new List<(int, VariableDefinition, TypeReference)>();

            for (var i = 0; i < newMethod.Parameters.Count; i++)
            {
                var param = newMethod.Parameters[i];
                var paramType = param.ParameterType;
                if (paramType.IsValueType || paramType.IsByReference && paramType.GetElementType().IsValueType)
                    body.Emit(OpCodes.Ldarg, i + argOffset);
                else
                {
                    var newParam = newMethod.Parameters[i];
                    if (newParam.ParameterType.IsByReference)
                    {
                        var scratchLocal = new VariableDefinition(imports.IntPtr);
                        newMethod.Body.Variables.Add(scratchLocal);
                                
                        byRefParams.Add((i, scratchLocal, newParam.ParameterType.GetElementType()));
                                
                        body.Emit(OpCodes.Ldarga, i + argOffset);
                        body.Emit(OpCodes.Ldloca, scratchLocal);
                        body.Emit(OpCodes.Call, imports.Module.ImportReference(new GenericInstanceMethod(imports.MarshalMethodParameterByRef) { GenericArguments = { newParam.ParameterType.GetElementType() } }));
                    }
                    else
                    {
                        body.Emit(OpCodes.Ldarga, i + argOffset);
                        body.Emit(OpCodes.Call, imports.Module.ImportReference(new GenericInstanceMethod(imports.MarshalMethodParameter) { GenericArguments = { newParam.ParameterType } }));
                    }
                }
            }

            body.Emit(OpCodes.Call, delegateType.Methods.Single(it => it.Name == "Invoke"));
            // todo: handle exceptions somehow? do icalls even throw them?
            
            foreach (var byRefParam in byRefParams)
            {
                var paramIndex = byRefParam.Item1;
                var paramVariable = byRefParam.Item2;
                body.Emit(OpCodes.Ldarga, paramIndex + argOffset);
                body.Emit(OpCodes.Ldloca, paramVariable);
                body.Emit(OpCodes.Call, imports.Module.ImportReference(new GenericInstanceMethod(imports.MarshalMethodParameterByRefRestore) { GenericArguments = { byRefParam.Item3 } }));
            }
            
            if (!newMethod.ReturnType.IsValueType && newMethod.ReturnType.FullName != "System.Void")
            {
                body.Emit(OpCodes.Call,
                    imports.Module.ImportReference(new GenericInstanceMethod(imports.MarshalMethodReturn)
                        {GenericArguments = {newMethod.ReturnType}}));
            }
            
            if (needScratchSpace)
            {
                newMethod.GenerateExitMethodCallFinallyBlock(imports);
                return;
            }
            
            body.Emit(OpCodes.Ret);
        }

        public static FieldDefinition GenerateStaticCtorSuffix(TypeDefinition enclosingType, TypeDefinition delegateType, MethodDefinition unityMethod, AssemblyKnownImports imports)
        {
            var delegateField = new FieldDefinition(delegateType.Name + "Field", FieldAttributes.Static | FieldAttributes.Private | FieldAttributes.InitOnly, delegateType);
            enclosingType.Fields.Add(delegateField);
            
            var staticCtor = enclosingType.Methods.SingleOrDefault(it => it.Name == ".cctor");
            if (staticCtor == null)
            {
                staticCtor = new MethodDefinition(".cctor",
                    MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, imports.Void);
                staticCtor.Body.GetILProcessor().Emit(OpCodes.Ret);
                enclosingType.Methods.Add(staticCtor);
            }
            var bodyProcessor = staticCtor.Body.GetILProcessor();

            bodyProcessor.Remove(staticCtor.Body.Instructions.Last()); // remove ret
            
            bodyProcessor.Emit(OpCodes.Ldstr, GetICallSignature(unityMethod));
            
            var methodRef = new GenericInstanceMethod(imports.Il2CppResolveICall);
            methodRef.GenericArguments.Add(delegateType);
            bodyProcessor.Emit(OpCodes.Call, enclosingType.Module.ImportReference(methodRef));
            bodyProcessor.Emit(OpCodes.Stsfld, delegateField);
            
            bodyProcessor.Emit(OpCodes.Ret); // restore ret

            return delegateField;
        }

        private static string GetICallSignature(MethodDefinition unityMethod)
        {
            var builder = new StringBuilder();
            builder.Append(unityMethod.DeclaringType.FullName);
            builder.Append("::");
            builder.Append(unityMethod.Name);

            return builder.ToString();
        }
    }
}