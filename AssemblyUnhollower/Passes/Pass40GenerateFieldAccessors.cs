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
                        if (typeContext.OriginalType.IsValueType && !fieldContext.OriginalField.IsStatic) continue;

                        var field = fieldContext.OriginalField;
                        var unmangleFieldName = fieldContext.UnmangledName;

                        var property = new PropertyDefinition(unmangleFieldName, PropertyAttributes.None,
                            assemblyContext.RewriteTypeRef(fieldContext.OriginalField.FieldType));
                        typeContext.NewType.Properties.Add(property);

                        var getter = FieldAccessorGenerator.MakeGetter(field, fieldContext.PointerField, property);
                        var setter = FieldAccessorGenerator.MakeSetter(field, fieldContext.PointerField, property);
                        typeContext.NewType.Methods.Add(getter);
                        typeContext.NewType.Methods.Add(setter);
                    }
                }
            }
        }
    }
}