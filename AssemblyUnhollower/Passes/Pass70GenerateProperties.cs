using System.Linq;
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

                        var property = new PropertyDefinition(unmangledPropertyName, PropertyAttributes.None,
                            assemblyContext.RewriteTypeRef(oldProperty.PropertyType));
                        typeContext.NewType.Properties.Add(property);

                        if (oldProperty.GetMethod != null)
                            property.GetMethod = typeContext.GetMethodByOldMethod(oldProperty.GetMethod).NewMethod;

                        if (oldProperty.SetMethod != null)
                            property.SetMethod = typeContext.GetMethodByOldMethod(oldProperty.SetMethod).NewMethod;
                    }
                }
            }
        }
        
        private static string UnmanglePropertyName(AssemblyRewriteContext assemblyContext, PropertyDefinition prop)
        {
            if (!prop.Name.IsObfuscated()) return prop.Name;

            return "prop_" + assemblyContext.RewriteTypeRef(prop.PropertyType).Name + "_" + prop.DeclaringType.Properties
                .Where(it => it.PropertyType.FullName == prop.PropertyType.FullName).ToList().IndexOf(prop);
        }
    }
}