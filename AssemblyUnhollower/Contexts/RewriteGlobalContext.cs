using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace AssemblyUnhollower.Contexts
{
    public class RewriteGlobalContext
    {
        private readonly Dictionary<string, AssemblyRewriteContext> myAssemblies = new Dictionary<string, AssemblyRewriteContext>();
        private readonly Dictionary<AssemblyDefinition, AssemblyRewriteContext> myAssembliesByOld = new Dictionary<AssemblyDefinition, AssemblyRewriteContext>();
        private readonly Resolver myAssemblyResolver = new Resolver();

        public IEnumerable<AssemblyRewriteContext> Assemblies => myAssemblies.Values;
        
        public RewriteGlobalContext(string mscorlibPath, IEnumerable<string> sourceAssemblyPaths)
        {
            var metadataResolver = new MetadataResolver(myAssemblyResolver);
            
            var mscorlib = AssemblyDefinition.ReadAssembly(mscorlibPath);
            TargetTypeSystemHandler.Init(mscorlib);
            
            myAssemblyResolver.Register(mscorlib);
            
            foreach (var sourceAssemblyPath in sourceAssemblyPaths)
            {
                var assemblyName = Path.GetFileNameWithoutExtension(sourceAssemblyPath);
                if(assemblyName == "Il2CppDummyDll") continue;
                
                var sourceAssembly = AssemblyDefinition.ReadAssembly(File.OpenRead(sourceAssemblyPath), new ReaderParameters() {MetadataResolver = metadataResolver});
                myAssemblyResolver.Register(sourceAssembly);
                var newAssembly = AssemblyDefinition.CreateAssembly(
                    new AssemblyNameDefinition(sourceAssembly.Name.Name.UnSystemify(), sourceAssembly.Name.Version),
                    sourceAssembly.MainModule.Name.UnSystemify(), sourceAssembly.MainModule.Kind);

                var assemblyRewriteContext = new AssemblyRewriteContext(this, sourceAssembly, newAssembly);
                myAssemblies[assemblyName] = assemblyRewriteContext;
                myAssembliesByOld[sourceAssembly] = assemblyRewriteContext;
            }
        }
        
        private class Resolver : DefaultAssemblyResolver
        {
            public void Register(AssemblyDefinition ass) => RegisterAssembly(ass);
        }

        public AssemblyRewriteContext GetNewAssemblyForOriginal(AssemblyDefinition oldAssembly)
        {
            return myAssembliesByOld[oldAssembly];
        }

        public TypeRewriteContext GetNewTypeForOriginal(TypeDefinition originalType)
        {
            return GetNewAssemblyForOriginal(originalType.Module.Assembly)
                .GetContextForOriginalType(originalType);
        }

        public AssemblyRewriteContext GetAssemblyByName(string name)
        {
            return myAssemblies[name];
        }
    }
}