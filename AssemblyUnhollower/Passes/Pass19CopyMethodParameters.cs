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
            foreach (var typeContext in assemblyContext.Types)
            {
                if (typeContext.RewriteSemantic is TypeRewriteContext.TypeRewriteSemantic.UseSystemInterface or TypeRewriteContext.TypeRewriteSemantic.UseSystemValueType) continue;
                
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

                        if (originalMethodParameter.HasConstant && originalMethodParameter.Constant is null or int
                                or byte or sbyte or char or short or ushort or uint or long or ulong or bool)
                            newParameter.Constant = originalMethodParameter.Constant;
                        else
                            newParameter.Attributes &= ~ParameterAttributes.HasDefault;

                        newMethod.Parameters.Add(newParameter);
                    }
                }
            }
            
            // overrides resolve requires parameters
            foreach (var assemblyContext in context.Assemblies)
            foreach (var typeContext in assemblyContext.Types)
            {
                if (typeContext.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemInterface || typeContext.RewriteSemantic == TypeRewriteContext.TypeRewriteSemantic.UseSystemValueType)
                    continue;
                
                foreach (var methodContext in typeContext.Methods)
                    methodContext.AssignExplicitOverrides();
            }
        }
    }
}