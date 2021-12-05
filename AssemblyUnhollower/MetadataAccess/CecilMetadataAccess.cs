using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;

namespace AssemblyUnhollower.MetadataAccess
{
    public class CecilMetadataAccess : IIl2CppMetadataAccess
    {
        private readonly Resolver myAssemblyResolver;
        private readonly List<AssemblyDefinition> myAssemblies = new();
        private readonly Dictionary<string, AssemblyDefinition> myAssembliesByName = new();
        private readonly Dictionary<(string AssemblyName, string TypeName), TypeDefinition> myTypesByName = new();
        
        public CecilMetadataAccess(IEnumerable<string> assemblyPaths, CecilMetadataAccess? parent = null)
        {
            myAssemblyResolver = new Resolver(parent?.myAssemblyResolver);
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
                
                if (sourceAssemblyName == "mscorlib")
                    PokeAllSystemTypes(sourceAssembly);
            }
        }

        // Occasionally cecil ends up with some system types having mismatched etype - fixup those
        private static void PokeAllSystemTypes(AssemblyDefinition assembly)
        {
            var typeSystem = assembly.MainModule.TypeSystem;
            foreach (var propertyInfo in typeof(TypeSystem).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (propertyInfo.PropertyType == typeof(TypeReference))
                    propertyInfo.GetMethod?.Invoke(typeSystem, Array.Empty<object>());
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
        
        internal class Resolver : IAssemblyResolver
        {
            private readonly Dictionary<string, AssemblyDefinition> myAssembliesByFullName = new();
            private readonly Dictionary<string, AssemblyDefinition> myAssembliesBySimpleName = new();

            private readonly Resolver? myParentResolver;

            public Resolver(Resolver? parentResolver = null)
            {
                myParentResolver = parentResolver;
            }

            public void Register(AssemblyDefinition ass)
            {
                myAssembliesByFullName[ass.FullName] = ass;
                myAssembliesBySimpleName[ass.Name.Name] = ass;
            }

            public void Dispose()
            {
                myAssembliesByFullName.Clear();
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                if (myAssembliesByFullName.TryGetValue(name.FullName, out var byFullName)) return byFullName;
                if (myAssembliesBySimpleName.TryGetValue(name.Name, out var bySimpleName)) return bySimpleName;

                return myParentResolver?.Resolve(name) ?? throw new KeyNotFoundException($"Assembly {name.FullName} not found");
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) => Resolve(name);
        }
    }
}