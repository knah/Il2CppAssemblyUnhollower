using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass24GenerateTypeStaticGetters
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                var il2CppTypeTypeRewriteContext = assemblyContext.GlobalContext
                    .GetAssemblyByName("mscorlib").GetTypeByName("System.Type");
                var il2CppSystemTypeRef =
                    assemblyContext.NewAssembly.MainModule.ImportReference(il2CppTypeTypeRewriteContext.NewType);
                
                foreach (var typeContext in assemblyContext.Types)
                {
                    if (typeContext.NewType.IsEnum) continue;
                    var typeGetMethod = new MethodDefinition("get_Il2CppType", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static, il2CppSystemTypeRef);
                    typeContext.NewType.Methods.Add(typeGetMethod);
                    var typeProperty = new PropertyDefinition("Il2CppType", PropertyAttributes.None, il2CppSystemTypeRef);
                    typeProperty.GetMethod = typeGetMethod;
                    typeContext.NewType.Properties.Add(typeProperty);

                    typeProperty.CustomAttributes.Add(new CustomAttribute(assemblyContext.Imports.ObsoleteAttributeCtor)
                    {
                        ConstructorArguments =
                        {
                            new CustomAttributeArgument(assemblyContext.Imports.String,
                                "Use Il2CppType.Of<T>() instead. This will be removed in a future version of unhollower.")
                        }
                    });
                    
                    var bodyBuilder = typeGetMethod.Body.GetILProcessor();
                    
                    bodyBuilder.Emit(OpCodes.Ldsfld, typeContext.ClassPointerFieldRef);
                    bodyBuilder.Emit(OpCodes.Call, assemblyContext.Imports.GetIl2CppTypeFromClass);

                    bodyBuilder.Emit(OpCodes.Call,
                        new MethodReference("internal_from_handle", il2CppSystemTypeRef,
                                il2CppSystemTypeRef)
                            {Parameters = {new ParameterDefinition(assemblyContext.Imports.IntPtr)}});
                    
                    bodyBuilder.Emit(OpCodes.Ret);
                }
            }
        }
    }
}