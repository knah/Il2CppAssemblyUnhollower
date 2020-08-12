using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower.Passes
{
    public static class Pass89GenerateForwarders
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            var targetAssembly = context.TryGetAssemblyByName("UnityEngine");
            if (targetAssembly == null)
            {
                LogSupport.Info("No UnityEngine.dll, will not generate forwarders");
                return;
            }

            var targetModule = targetAssembly.NewAssembly.MainModule;
            
            foreach (var assemblyRewriteContext in context.Assemblies)
            {
                if (!assemblyRewriteContext.NewAssembly.Name.Name.StartsWith("UnityEngine.")) continue;
                foreach (var mainModuleType in assemblyRewriteContext.NewAssembly.MainModule.Types)
                {
                    var importedType = targetModule.ImportReference(mainModuleType);
                    var exportedType = new ExportedType(mainModuleType.Namespace, mainModuleType.Name, importedType.Module, importedType.Scope) { Attributes = TypeAttributes.Forwarder };
                    targetModule.ExportedTypes.Add(exportedType);

                    AddNestedTypes(mainModuleType, exportedType, targetModule);
                }
            }
        }

        private static void AddNestedTypes(TypeDefinition mainModuleType, ExportedType importedType, ModuleDefinition targetModule)
        {
            foreach (var nested in mainModuleType.NestedTypes)
            {
                if((nested.Attributes & TypeAttributes.VisibilityMask) != TypeAttributes.NestedPublic) continue;
                
                var nestedImport = targetModule.ImportReference(nested);
                var nestedExport = new ExportedType(nestedImport.Namespace, nestedImport.Name, nestedImport.Module, nestedImport.Scope) { Attributes = TypeAttributes.Forwarder };
                nestedExport.DeclaringType = importedType; 
                targetModule.ExportedTypes.Add(nestedExport);
                
                AddNestedTypes(nested, nestedExport, targetModule);
            }
        }
    }
}