using AssemblyUnhollower.Contexts;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass40GenerateFieldAccessors
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var typeContext in assemblyContext.Types)
                {
                    foreach (var fieldContext in typeContext.Fields)
                    {
                        if (typeContext.ComputedTypeSpecifics == TypeRewriteContext.TypeSpecifics.BlittableStruct && !fieldContext.OriginalField.IsStatic) continue;

                        var field = fieldContext.OriginalField;
                        var unmangleFieldName = fieldContext.UnmangledName;

                        var property = new PropertyDefinition(unmangleFieldName, PropertyAttributes.None,
                            assemblyContext.RewriteTypeRef(fieldContext.OriginalField.FieldType));
                        typeContext.NewType.Properties.Add(property);

                        FieldAccessorGenerator.MakeGetter(field, fieldContext, property);
                        FieldAccessorGenerator.MakeSetter(field, fieldContext, property);
                    }
                }
            }
        }
    }
}