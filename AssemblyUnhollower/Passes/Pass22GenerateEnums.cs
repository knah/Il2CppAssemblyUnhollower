using AssemblyUnhollower.Contexts;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass22GenerateEnums
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var typeContext in assemblyContext.Types)
                {
                    if (!typeContext.OriginalType.IsEnum) continue;

                    // todo: flags attribute
                    
                    var type = typeContext.OriginalType;
                    var newType = typeContext.NewType;
                    foreach (var fieldDefinition in type.Fields)
                    {
                        var fieldName = fieldDefinition.Name;
                        if (fieldName.IsObfuscated())
                            fieldName = "EnumValue" + fieldDefinition.Constant;
                        
                        var newDef = new FieldDefinition(fieldName, fieldDefinition.Attributes, assemblyContext.RewriteTypeRef(fieldDefinition.FieldType));
                        newType.Fields.Add(newDef);

                        newDef.Constant = fieldDefinition.Constant;
                    }
                }
            }
        }
    }
}