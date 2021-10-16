using System;
using System.Collections.Generic;
using AssemblyUnhollower.Extensions;
using AssemblyUnhollower.MetadataAccess;
using Mono.Cecil;

namespace AssemblyUnhollower.Contexts
{
    public class RewriteGlobalContext : IDisposable
    {
        public UnhollowerOptions Options { get; }
        public IIl2CppMetadataAccess GameAssemblies { get; }
        public IMetadataAccess SystemAssemblies { get; }
        public IMetadataAccess UnityAssemblies { get; }

        private readonly CecilMetadataAccess.Resolver myNewAssembliesResolver = new();
        internal readonly MetadataResolver NewMetadataResolver;

        private readonly Dictionary<string, AssemblyRewriteContext> myAssemblies = new Dictionary<string, AssemblyRewriteContext>();
        private readonly Dictionary<AssemblyDefinition, AssemblyRewriteContext> myAssembliesByOld = new Dictionary<AssemblyDefinition, AssemblyRewriteContext>();
        
        internal readonly Dictionary<(object, string, int), List<TypeDefinition>> RenameGroups = new Dictionary<(object, string, int), List<TypeDefinition>>();
        internal readonly Dictionary<TypeDefinition, string> RenamedTypes = new Dictionary<TypeDefinition, string>();
        internal readonly Dictionary<TypeDefinition, string> PreviousRenamedTypes = new Dictionary<TypeDefinition, string>();

        internal readonly List<long> MethodStartAddresses = new List<long>();

        public IEnumerable<AssemblyRewriteContext> Assemblies => myAssemblies.Values;
        
        internal bool HasGcWbarrierFieldWrite { get; set; }
        
        public RewriteGlobalContext(UnhollowerOptions options, IIl2CppMetadataAccess gameAssemblies, IMetadataAccess systemAssemblies, IMetadataAccess unityAssemblies)
        {
            Options = options;
            GameAssemblies = gameAssemblies;
            SystemAssemblies = systemAssemblies;
            UnityAssemblies = unityAssemblies;

            NewMetadataResolver = new MetadataResolver(myNewAssembliesResolver);

            TargetTypeSystemHandler.Init(systemAssemblies);
            
            foreach (var sourceAssembly in gameAssemblies.Assemblies)
            {
                var assemblyName = sourceAssembly.Name.Name;
                if (assemblyName == "Il2CppDummyDll") continue;
                
                var newAssembly = AssemblyDefinition.CreateAssembly(
                    new AssemblyNameDefinition(sourceAssembly.Name.Name.UnSystemify(), sourceAssembly.Name.Version),
                    sourceAssembly.MainModule.Name.UnSystemify(), new ModuleParameters { Kind = sourceAssembly.MainModule.Kind, MetadataResolver = NewMetadataResolver });

                var assemblyRewriteContext = new AssemblyRewriteContext(this, sourceAssembly, newAssembly);
                AddAssemblyContext(assemblyName, assemblyRewriteContext);
            }
        }

        internal void AddAssemblyContext(string assemblyName, AssemblyRewriteContext context)
        {
            myAssemblies[assemblyName] = context;
            myNewAssembliesResolver.Register(context.NewAssembly);
            if (context.OriginalAssembly != null)
                myAssembliesByOld[context.OriginalAssembly] = context;
        }

        public AssemblyRewriteContext GetNewAssemblyForOriginal(AssemblyDefinition oldAssembly)
        {
            return myAssemblies[oldAssembly.Name.Name];
        }

        public TypeRewriteContext GetNewTypeForOriginal(TypeDefinition originalType)
        {
            return GetNewAssemblyForOriginal(originalType.Module.Assembly)
                .GetContextForOriginalType(originalType);
        }
        
        public TypeRewriteContext? TryGetNewTypeForOriginal(TypeDefinition originalType)
        {
            if (!myAssembliesByOld.TryGetValue(originalType.Module.Assembly, out var assembly))
                return null;
            return assembly.TryGetContextForOriginalType(originalType);
        }
        
        public TypeRewriteContext.TypeSpecifics JudgeSpecificsByOriginalType(TypeReference typeRef)
        {
            if (typeRef.IsPrimitive || typeRef.IsPointer || typeRef.FullName == "System.TypedReference") return TypeRewriteContext.TypeSpecifics.BlittableStruct;
            if (typeRef.FullName == "System.String" || typeRef.FullName == "System.Object" || typeRef.IsArray || typeRef.IsByReference || typeRef.IsGenericParameter || typeRef.IsGenericInstance)
                return TypeRewriteContext.TypeSpecifics.ReferenceType;

            var fieldTypeContext = GetNewTypeForOriginal(typeRef.Resolve());
            return fieldTypeContext.ComputedTypeSpecifics;
        }

        public AssemblyRewriteContext GetAssemblyByName(string name)
        {
            return myAssemblies[name];
        }
        
        public AssemblyRewriteContext? TryGetAssemblyByName(string name)
        {
            if (myAssemblies.TryGetValue(name, out var result))
                return result;

            if (name == "netstandard")
                return myAssemblies.TryGetValue("mscorlib", out var result2) ? result2 : null;
            
            return null;
        }

        public void Dispose()
        {
            foreach (var assembly in Assemblies)
            {
                assembly.NewAssembly.Dispose();
                assembly.OriginalAssembly.Dispose();
            }
        }

        public readonly StatisticsStore Statistics = new();

        public class StatisticsStore
        {
            public int SystemInterfaceCandidates;
            public int EligibleSystemInterfaces;
            public int TokenLessMethods;
        }
    }
}