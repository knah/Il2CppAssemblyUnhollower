using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass19CopyMethodParameters
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

                        foreach (var originalMethodParameter in originalMethod.Parameters)
                        {
                            var newName = originalMethodParameter.Name.IsObfuscated(context.Options)
                                ? $"param_{originalMethodParameter.Sequence}"
                                : originalMethodParameter.Name;
                            
                            var newParameter = new ParameterDefinition(newName,
                                originalMethodParameter.Attributes & ~ParameterAttributes.HasFieldMarshal,
                                assemblyContext.RewriteTypeRef(originalMethodParameter.ParameterType));

                            if (originalMethodParameter.HasConstant && (originalMethodParameter.Constant == null ||
                                                                        originalMethodParameter.Constant is string ||
                                                                        originalMethodParameter.Constant is bool))
                                newParameter.Constant = originalMethodParameter.Constant;
                            else
                                newParameter.Attributes &= ~ParameterAttributes.HasDefault;

                            newMethod.Parameters.Add(newParameter);
                        }
                    }
                }
            }
        }
    }
}