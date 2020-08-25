using System.Linq;
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

                    var type = typeContext.OriginalType;
                    var newType = typeContext.NewType;
                    
                    if (type.CustomAttributes.Any(it => it.AttributeType.FullName == "System.FlagsAttribute"))
                        newType.CustomAttributes.Add(new CustomAttribute(assemblyContext.Imports.FlagsAttributeCtor));

                    foreach (var fieldDefinition in type.Fields)
                    {
                        var fieldName = fieldDefinition.Name;
                        if (fieldName.IsInvalidInSource())
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