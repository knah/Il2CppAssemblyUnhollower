using System.Linq;
using System.Reflection;
using AssemblyUnhollower.Contexts;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass70GenerateProperties
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var typeContext in assemblyContext.Types)
                {
                    var type = typeContext.OriginalType;
                    foreach (var oldProperty in type.Properties)
                    {
                        var unmangledPropertyName = UnmanglePropertyName(assemblyContext, oldProperty);

                        var property = new PropertyDefinition(unmangledPropertyName, oldProperty.Attributes,
                            assemblyContext.RewriteTypeRef(oldProperty.PropertyType));
                        foreach (var oldParameter in oldProperty.Parameters)
                            property.Parameters.Add(new ParameterDefinition(oldParameter.Name, oldParameter.Attributes,
                                assemblyContext.RewriteTypeRef(oldParameter.ParameterType)));
                        
                        typeContext.NewType.Properties.Add(property);

                        if (oldProperty.GetMethod != null)
                            property.GetMethod = typeContext.GetMethodByOldMethod(oldProperty.GetMethod).NewMethod;

                        if (oldProperty.SetMethod != null)
                            property.SetMethod = typeContext.GetMethodByOldMethod(oldProperty.SetMethod).NewMethod;
                    }

                    var defaultMemberAttribute = type.CustomAttributes.FirstOrDefault(it =>
                        it.AttributeType.Name == "AttributeAttribute" && it.Fields.Any(it => it.Name == "Name" && (string) it.Argument.Value == nameof(DefaultMemberAttribute)));
                    if (defaultMemberAttribute != null)
                    {
                        typeContext.NewType.CustomAttributes.Add(new CustomAttribute(
                            new MethodReference(".ctor", assemblyContext.Imports.Void,
                                assemblyContext.Imports.DefaultMemberAttribute)
                            {
                                HasThis = true,
                                Parameters = {new ParameterDefinition(assemblyContext.Imports.String)}
                            })
                        {
                            ConstructorArguments = { new CustomAttributeArgument(assemblyContext.Imports.String, "Item") }
                        });
                    }
                }
            }
        }
        
        private static string UnmanglePropertyName(AssemblyRewriteContext assemblyContext, PropertyDefinition prop)
        {
            if (!prop.Name.IsObfuscated()) return prop.Name;

            return "prop_" + assemblyContext.RewriteTypeRef(prop.PropertyType).GetUnmangledName() + "_" + prop.DeclaringType.Properties
                .Where(it => it.PropertyType.GetUnmangledName() == prop.PropertyType.GetUnmangledName()).ToList().IndexOf(prop);
        }
    }
}