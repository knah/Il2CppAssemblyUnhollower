using System.Collections.Generic;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace AssemblyUnhollower.MetadataAccess
{
    public class NullMetadataAccess : IMetadataAccess
    {
        public static readonly NullMetadataAccess Instance = new();
        
        public void Dispose()
        {
        }

        public IList<AssemblyDefinition> Assemblies => ReadOnlyCollection<AssemblyDefinition>.Empty;
        public AssemblyDefinition? GetAssemblyBySimpleName(string name) => null;
        public TypeDefinition? GetTypeByName(string assemblyName, string typeName) => null;
        public IList<GenericInstanceType>? GetKnownInstantiationsFor(TypeReference genericDeclaration) => null;
        public string? GetStringStoredAtAddress(long offsetInMemory) => null;
        public MethodReference? GetMethodRefStoredAt(long offsetInMemory) => null;
    }
}