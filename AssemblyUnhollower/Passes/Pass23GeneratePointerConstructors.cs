using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass23GeneratePointerConstructors
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var typeContext in assemblyContext.Types)
                {
                    if (typeContext.ComputedTypeSpecifics == TypeRewriteContext.TypeSpecifics.BlittableStruct || typeContext.OriginalType.IsEnum) continue;
                    //if (typeContext.RewriteSemantic != TypeRewriteContext.TypeRewriteSemantic.Default) continue;//todo: uncomment

                    var newType = typeContext.NewType;
                    var nativeCtor = new MethodDefinition(".ctor",
                        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName |
                        MethodAttributes.HideBySig, assemblyContext.Imports.Void);

                    nativeCtor.Parameters.Add(new ParameterDefinition(assemblyContext.Imports.IntPtr));
                    
                    var ctorBody = nativeCtor.Body.GetILProcessor();
                    newType.Methods.Add(nativeCtor);

                    ctorBody.Emit(OpCodes.Ldarg_0);
                    ctorBody.Emit(OpCodes.Ldarg_1);
                    ctorBody.Emit(OpCodes.Call,
                        new MethodReference(".ctor", assemblyContext.Imports.Void, newType.BaseType)
                            {Parameters = {new ParameterDefinition(assemblyContext.Imports.IntPtr)}, HasThis = true});
                    ctorBody.Emit(OpCodes.Ret);
                }
            }
        }
    }
}