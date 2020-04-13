using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass60AddImplicitConversions
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            var assemblyContext = context.GetAssemblyByName("mscorlib");
            var typeContext = assemblyContext.GetTypeByName("System.String");
            
            var methodFromMonoString = new MethodDefinition("op_Implicit", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeContext.NewType);
            methodFromMonoString.Parameters.Add(new ParameterDefinition(assemblyContext.Imports.String));
            typeContext.NewType.Methods.Add(methodFromMonoString);
            var fromBuilder = methodFromMonoString.Body.GetILProcessor();

            var createIl2CppStringNop = fromBuilder.Create(OpCodes.Nop);
            
            fromBuilder.Emit(OpCodes.Ldarg_0);
            fromBuilder.Emit(OpCodes.Dup);
            fromBuilder.Emit(OpCodes.Brtrue_S, createIl2CppStringNop);
            fromBuilder.Emit(OpCodes.Ret);
            
            fromBuilder.Append(createIl2CppStringNop);
            fromBuilder.Emit(OpCodes.Call, assemblyContext.Imports.StringToNative);
            fromBuilder.Emit(OpCodes.Newobj,
                new MethodReference(".ctor", assemblyContext.Imports.Void, typeContext.NewType)
                {
                    HasThis = true, Parameters = {new ParameterDefinition(assemblyContext.Imports.IntPtr)}
                });
            fromBuilder.Emit(OpCodes.Ret);
            
            
            var methodToMonoString = new MethodDefinition("op_Implicit", MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, assemblyContext.Imports.String);
            methodToMonoString.Parameters.Add(new ParameterDefinition(typeContext.NewType));
            typeContext.NewType.Methods.Add(methodToMonoString);
            var toBuilder = methodToMonoString.Body.GetILProcessor();

            var createStringNop = toBuilder.Create(OpCodes.Nop);
            
            toBuilder.Emit(OpCodes.Ldarg_0);
            toBuilder.Emit(OpCodes.Call, assemblyContext.Imports.Il2CppObjectBaseToPointer);
            toBuilder.Emit(OpCodes.Dup);
            toBuilder.Emit(OpCodes.Brtrue_S, createStringNop);
            toBuilder.Emit(OpCodes.Pop);
            toBuilder.Emit(OpCodes.Ldnull);
            toBuilder.Emit(OpCodes.Ret);
            
            toBuilder.Append(createStringNop);
            toBuilder.Emit(OpCodes.Call, assemblyContext.Imports.StringFromNative);
            toBuilder.Emit(OpCodes.Ret);
        }
    }
}