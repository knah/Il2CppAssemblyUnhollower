using System.Collections.Generic;
using Mono.Cecil;

namespace AssemblyUnhollower.MetadataAccess
{
    public class CecilMetadataAccess : IIl2CppMetadataAccess
    {
        private readonly Resolver myAssemblyResolver = new();
        private readonly List<AssemblyDefinition> myAssemblies = new();
        private readonly Dictionary<string, AssemblyDefinition> myAssembliesByName = new();
        private readonly Dictionary<(string AssemblyName, string TypeName), TypeDefinition> myTypesByName = new();
        
        public CecilMetadataAccess(IEnumerable<string> assemblyPaths)
        {
            var metadataResolver = new MetadataResolver(myAssemblyResolver);
            
            foreach (var sourceAssemblyPath in assemblyPaths)
            {
                var sourceAssembly = AssemblyDefinition.ReadAssembly(sourceAssemblyPath, new ReaderParameters(ReadingMode.Deferred) {MetadataResolver = metadataResolver});
                myAssemblyResolver.Register(sourceAssembly);
                myAssemblies.Add(sourceAssembly);
                myAssembliesByName[sourceAssembly.Name.Name] = sourceAssembly;
            }
            
            foreach (var sourceAssembly in myAssemblies)
            {
                var sourceAssemblyName = sourceAssembly.Name.Name;
                foreach (var type in sourceAssembly.MainModule.Types)
                    myTypesByName[(sourceAssemblyName, type.FullName)] = type;
            }
        }

        public void Dispose()
        {
            foreach (var assemblyDefinition in myAssemblies) 
                assemblyDefinition.Dispose();
            
            myAssemblies.Clear();
            myAssembliesByName.Clear();
            myAssemblyResolver.Dispose();
        }

        public AssemblyDefinition? GetAssemblyBySimpleName(string name) => myAssembliesByName.TryGetValue(name, out var result) ? result : null;

        public TypeDefinition? GetTypeByName(string assemblyName, string typeName) => myTypesByName.TryGetValue((assemblyName, typeName), out var result) ? result : null;

        public IList<AssemblyDefinition> Assemblies => myAssemblies;

        public IList<GenericInstanceType>? GetKnownInstantiationsFor(TypeDefinition genericDeclaration) => null;
        public string? GetStringStoredAtAddress(long offsetInMemory) => null;
        public MethodReference? GetMethodRefStoredAt(long offsetInMemory) => null;
        
        internal class Resolver : DefaultAssemblyResolver
        {
            public void Register(AssemblyDefinition ass) => RegisterAssembly(ass);
        }
    }
}