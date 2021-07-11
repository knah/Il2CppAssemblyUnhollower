using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
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
                    var propertyCountsByName = new Dictionary<string, int>();
                    
                    foreach (var oldProperty in type.Properties)
                    {
                        var unmangledPropertyName = UnmanglePropertyName(assemblyContext, oldProperty, typeContext.NewType, propertyCountsByName);

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

                    string? defaultMemberName = null;
                    var defaultMemberAttributeAttribute = type.CustomAttributes.FirstOrDefault(it =>
                        it.AttributeType.Name == "AttributeAttribute" && it.Fields.Any(it =>
                            it.Name == "Name" && (string)it.Argument.Value == nameof(DefaultMemberAttribute)));
                    if (defaultMemberAttributeAttribute != null)
                        defaultMemberName = "Item";
                    else
                    {
                        var realDefaultMemberAttribute = type.CustomAttributes.FirstOrDefault(it => it.AttributeType.Name == nameof(DefaultMemberAttribute));
                        if (realDefaultMemberAttribute != null)
                            defaultMemberName = realDefaultMemberAttribute.ConstructorArguments[0].Value.ToString();
                    }

                    if (defaultMemberName != null)
                    {
                        typeContext.NewType.CustomAttributes.Add(new CustomAttribute(
                            new MethodReference(".ctor", assemblyContext.Imports.Void,
                                assemblyContext.Imports.DefaultMemberAttribute)
                            {
                                HasThis = true,
                                Parameters = {new ParameterDefinition(assemblyContext.Imports.String)}
                            })
                        {
                            ConstructorArguments = { new CustomAttributeArgument(assemblyContext.Imports.String, defaultMemberName) }
                        });
                    }
                }
            }
        }
        
        private static string UnmanglePropertyName(AssemblyRewriteContext assemblyContext, PropertyDefinition prop, TypeReference declaringType, Dictionary<string, int> countsByBaseName)
        {
            if (assemblyContext.GlobalContext.Options.PassthroughNames || !prop.Name.IsObfuscated(assemblyContext.GlobalContext.Options)) return prop.Name;

            var baseName = "prop_" + assemblyContext.RewriteTypeRef(prop.PropertyType).GetUnmangledName();

            countsByBaseName.TryGetValue(baseName, out var index);
            countsByBaseName[baseName] = index + 1;
            
            var unmanglePropertyName = baseName + "_" + index;
                        
            if (assemblyContext.GlobalContext.Options.RenameMap.TryGetValue(declaringType.GetNamespacePrefix() + "::" + unmanglePropertyName, out var newName))
                unmanglePropertyName = newName;
            
            return unmanglePropertyName;
        }
    }
}