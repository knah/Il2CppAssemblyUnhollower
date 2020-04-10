using AssemblyUnhollower.Contexts;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass11FillTypedefs
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var typeContext in assemblyContext.Types)
                {
                    foreach (var originalParameter in typeContext.OriginalType.GenericParameters)
                    {
                        var newParameter = new GenericParameter(originalParameter.Name, typeContext.NewType);
                        typeContext.NewType.GenericParameters.Add(newParameter);
                        newParameter.Attributes = originalParameter.Attributes;
                    }

                    if (typeContext.OriginalType.IsEnum)
                    {
                        typeContext.NewType.BaseType = assemblyContext.Imports.Enum;
                    } else if (typeContext.OriginalType.IsValueType) {
                        typeContext.NewType.BaseType = assemblyContext.Imports.ValueType;
                    } else
                        typeContext.NewType.BaseType = assemblyContext.RewriteTypeRef(typeContext.OriginalType.BaseType);
                }
            }
        }
    }
}