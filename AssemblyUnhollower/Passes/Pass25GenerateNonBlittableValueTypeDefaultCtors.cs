using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass25GenerateNonBlittableValueTypeDefaultCtors
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var typeContext in assemblyContext.Types)
                {
                    if (typeContext.ComputedTypeSpecifics !=
                        TypeRewriteContext.TypeSpecifics.NonBlittableStruct) continue;

                    var emptyCtor = new MethodDefinition(".ctor",
                        MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName |
                        MethodAttributes.HideBySig, assemblyContext.Imports.Void);
                    
                    typeContext.NewType.Methods.Add(emptyCtor);

                    var bodyBuilder = emptyCtor.Body.GetILProcessor();
                    bodyBuilder.Emit(OpCodes.Ldsfld, typeContext.ClassPointerFieldRef);
                    bodyBuilder.Emit(OpCodes.Dup);
                    bodyBuilder.Emit(OpCodes.Ldc_I4_0);
                    bodyBuilder.Emit(OpCodes.Conv_U);
                    bodyBuilder.Emit(OpCodes.Call, assemblyContext.Imports.ValueSizeGet);
                    bodyBuilder.Emit(OpCodes.Conv_U);
                    bodyBuilder.Emit(OpCodes.Localloc);
                    bodyBuilder.Emit(OpCodes.Call, assemblyContext.Imports.ObjectBox);
                    bodyBuilder.Emit(OpCodes.Call, new MethodReference(".ctor", assemblyContext.Imports.Void, typeContext.NewType.BaseType) { HasThis = true, Parameters = { new ParameterDefinition(assemblyContext.Imports.IntPtr) }});
                    bodyBuilder.Emit(OpCodes.Ret);
                }
            }
        }
    }
}