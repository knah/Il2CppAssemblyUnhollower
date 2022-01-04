using System.Linq;
using System.Text;
using AssemblyUnhollower.Contexts;
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
                    convertedParameter.Attributes & ~ParameterAttributes.Optional,
                    convertedParameter.ParameterType.IsValueType ? convertedParameter.ParameterType : imports.IntPtr));

            return delegateType;
        }

        public static void GenerateInvokerMethodBody(MethodDefinition newMethod, FieldDefinition delegateField, TypeDefinition delegateType, TypeRewriteContext enclosingType, AssemblyKnownImports imports)
        {
            var body = newMethod.Body.GetILProcessor();
            
            body.Emit(OpCodes.Ldsfld, delegateField);
            if (newMethod.HasThis)
            {
                body.Emit(OpCodes.Ldarg_0);
                body.Emit(OpCodes.Call, imports.Il2CppObjectBaseToPointerNotNull);
            }

            var argOffset = newMethod.HasThis ? 1 : 0;

            for (var i = 0; i < newMethod.Parameters.Count; i++)
            {
                var param = newMethod.Parameters[i];
                var paramType = param.ParameterType;
                if(paramType.IsValueType || paramType.IsByReference && paramType.GetElementType().IsValueType)
                    body.Emit(OpCodes.Ldarg, i + argOffset);
                else
                {
                    body.EmitObjectToPointer(param.ParameterType, param.ParameterType, enclosingType, i + argOffset, false, true, true, out var refVar);
                    if (refVar != null)
                        LogSupport.Trace($"Method {newMethod} has a reference-typed ref parameter, this will be ignored");
                }
            }

            body.Emit(OpCodes.Call, delegateType.Methods.Single(it => it.Name == "Invoke"));
            if (!newMethod.ReturnType.IsValueType)
            {
                var pointerVar = new VariableDefinition(imports.IntPtr);
                newMethod.Body.Variables.Add(pointerVar);
                body.Emit(OpCodes.Stloc, pointerVar);
                var loadInstr = body.Create(OpCodes.Ldloc, pointerVar);
                body.EmitPointerToObject(newMethod.ReturnType, newMethod.ReturnType, enclosingType, loadInstr, false, false);
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