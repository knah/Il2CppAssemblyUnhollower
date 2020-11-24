using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass12FillTypedefs
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
                        newParameter.Attributes = originalParameter.Attributes.StripValueTypeConstraint();
                    }

                    if (typeContext.OriginalType.IsEnum)
                    {
                        typeContext.NewType.BaseType = assemblyContext.Imports.Enum;
                    } else if (typeContext.ComputedTypeSpecifics == TypeRewriteContext.TypeSpecifics.BlittableStruct) {
                        typeContext.NewType.BaseType = assemblyContext.Imports.ValueType;
                    }
                }
            }
            
            // Second pass is explicitly done after first to account for rewriting of generic base types - value-typeness is important there
            foreach (var assemblyContext in context.Assemblies)
            foreach (var typeContext in assemblyContext.Types)
                if (!typeContext.OriginalType.IsEnum && typeContext.ComputedTypeSpecifics !=
                    TypeRewriteContext.TypeSpecifics.BlittableStruct)
                    typeContext.NewType.BaseType = assemblyContext.RewriteTypeRef(typeContext.OriginalType.BaseType);
        }
    }
}