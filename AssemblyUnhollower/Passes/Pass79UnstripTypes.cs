using System.IO;
using System.Linq;
using AssemblyUnhollower.Contexts;
using Mono.Cecil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower.Passes
{
    public static class Pass79UnstripTypes
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            var unityAssemblyFiles = Directory.EnumerateFiles(context.Options.UnityBaseLibsDir, "*.dll");
            var loadedAssemblies = unityAssemblyFiles.Select(it =>
                AssemblyDefinition.ReadAssembly(it, new ReaderParameters(ReadingMode.Deferred))).ToList();

            var typesUnstripped = 0;
            
            foreach (var unityAssembly in loadedAssemblies)
            {
                var processedAssembly = context.TryGetAssemblyByName(unityAssembly.Name.Name);
                if (processedAssembly == null) continue;
                var imports = processedAssembly.Imports;
                
                foreach (var unityType in unityAssembly.MainModule.Types)
                {
                    ProcessType(processedAssembly, unityType, null, imports, ref typesUnstripped);
                }
            }
            
            LogSupport.Trace(""); // end the progress message
            LogSupport.Trace($"{typesUnstripped} types restored");
        }

        private static void ProcessType(AssemblyRewriteContext processedAssembly, TypeDefinition unityType,
            TypeDefinition? enclosingNewType, AssemblyKnownImports imports, ref int typesUnstripped)
        {
            var processedType = enclosingNewType == null ? processedAssembly.TryGetTypeByName(unityType.FullName)?.NewType : enclosingNewType.NestedTypes.SingleOrDefault(it => it.Name == unityType.Name);
            if (unityType.IsEnum)
            {
                if (processedType != null) return;

                typesUnstripped++;
                var clonedType = CloneEnum(unityType, imports);
                if (enclosingNewType == null)
                    processedAssembly.NewAssembly.MainModule.Types.Add(clonedType);
                else
                {
                    enclosingNewType.NestedTypes.Add(clonedType);
                    clonedType.DeclaringType = enclosingNewType;
                }

                processedAssembly.RegisterTypeRewrite(new TypeRewriteContext(processedAssembly, null, clonedType));
                
                return;
            }

            if (unityType.IsSealed && unityType.IsAbstract && processedType == null) // aka static
            {
                typesUnstripped++;
                var clonedType = CloneStatic(unityType, imports);
                if (enclosingNewType == null)
                    processedAssembly.NewAssembly.MainModule.Types.Add(clonedType);
                else
                {
                    enclosingNewType.NestedTypes.Add(clonedType);
                    clonedType.DeclaringType = enclosingNewType;
                }

                processedAssembly.RegisterTypeRewrite(new TypeRewriteContext(processedAssembly, null, clonedType));
            }

            foreach (var nestedUnityType in unityType.NestedTypes)
                ProcessType(processedAssembly, nestedUnityType, processedType, imports, ref typesUnstripped);
        }

        private static TypeDefinition CloneEnum(TypeDefinition sourceEnum, AssemblyKnownImports imports)
        {
            var newType = new TypeDefinition(sourceEnum.Namespace, sourceEnum.Name, ForcePublic(sourceEnum.Attributes), imports.Enum);
            foreach (var sourceEnumField in sourceEnum.Fields)
            {
                var newField = new FieldDefinition(sourceEnumField.Name, sourceEnumField.Attributes, sourceEnumField.Name == "value__" ? TargetTypeSystemHandler.String.Module.GetType(sourceEnumField.FieldType.FullName) : newType);
                newField.Constant = sourceEnumField.Constant;
                newType.Fields.Add(newField);
            }

            return newType;
        }
        
        private static TypeDefinition CloneStatic(TypeDefinition sourceEnum, AssemblyKnownImports imports)
        {
            var newType = new TypeDefinition(sourceEnum.Namespace, sourceEnum.Name, ForcePublic(sourceEnum.Attributes), imports.Object);
            return newType;
        }

        private static TypeAttributes ForcePublic(TypeAttributes typeAttributes)
        {
            var visibility = typeAttributes & TypeAttributes.VisibilityMask;
            if (visibility == 0 || visibility == TypeAttributes.Public)
                return typeAttributes | TypeAttributes.Public;
            
            return typeAttributes & ~(TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
        }
    }
}