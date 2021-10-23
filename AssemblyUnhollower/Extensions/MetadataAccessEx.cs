using AssemblyUnhollower.MetadataAccess;
using Mono.Cecil;

namespace AssemblyUnhollower.Extensions
{
    public static class MetadataAccessEx
    {
        public static TypeDefinition? GetTypeByOriginalType(this IMetadataAccess metadataAccess, TypeDefinition originalType)
        {
            var typeFullName = originalType.GetFullNameWithNesting();
            var typeAssemblyName = originalType.Module.Assembly.Name.Name;

            return metadataAccess.GetTypeByName(typeAssemblyName, typeFullName);
        }

        public static TypeDefinition? GetTypeByOriginalTypeAnyAssembly(this IMetadataAccess metadataAccess, TypeDefinition originalType)
        {
            var typeFullName = originalType.GetFullNameWithNesting();

            foreach (var metadataAccessAssembly in metadataAccess.Assemblies)
            {
                var result = metadataAccess.GetTypeByName(metadataAccessAssembly.Name.Name, typeFullName);
                if (result != null) return result;
            }

            return null;
        }

        public static TypeDefinition? GetTypeByOriginalTypeOwnAssemblyFirst(this IMetadataAccess metadataAccess, TypeDefinition originalType)
        {
            return GetTypeByOriginalType(metadataAccess, originalType) ??
                   GetTypeByOriginalTypeAnyAssembly(metadataAccess, originalType);
        }
    }
}
