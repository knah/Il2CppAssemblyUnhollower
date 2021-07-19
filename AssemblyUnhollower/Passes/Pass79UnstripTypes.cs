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
            var typesUnstripped = 0;
            
            foreach (var unityAssembly in context.UnityAssemblies.Assemblies)
            {
                var processedAssembly = context.TryGetAssemblyByName(unityAssembly.Name.Name);
                if (processedAssembly == null)
                {
                    var newAssembly = new AssemblyRewriteContext(context, unityAssembly,
                        AssemblyDefinition.CreateAssembly(unityAssembly.Name, unityAssembly.MainModule.Name,
                            ModuleKind.Dll));
                    context.AddAssemblyContext(unityAssembly.Name.Name, newAssembly);
                    processedAssembly = newAssembly;
                }
                var imports = processedAssembly.Imports;
                
                foreach (var unityType in unityAssembly.MainModule.Types)
                    ProcessType(processedAssembly, unityType, null, imports, ref typesUnstripped);
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

                processedAssembly.RegisterTypeRewrite(new TypeRewriteContext(processedAssembly, null, clonedType, null));
                
                return;
            }

            if (processedType == null && !unityType.IsEnum && !HasNonBlittableFields(unityType) && !unityType.HasGenericParameters) // restore all types even if it would be not entirely correct
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

                processedAssembly.RegisterTypeRewrite(new TypeRewriteContext(processedAssembly, null, clonedType, null));
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

        private static bool HasNonBlittableFields(TypeDefinition type)
        {
            if (!type.IsValueType) return false;

            foreach (var fieldDefinition in type.Fields)
            {
                if (fieldDefinition.IsStatic || fieldDefinition.FieldType == type) continue;

                if (!fieldDefinition.FieldType.IsValueType)
                    return true;

                if (fieldDefinition.FieldType.Namespace.StartsWith("System") && HasNonBlittableFields(fieldDefinition.FieldType.Resolve()))
                    return true;
            }

            return false;
        }

        private static TypeDefinition CloneStatic(TypeDefinition sourceEnum, AssemblyKnownImports imports)
        {
            var newType = new TypeDefinition(sourceEnum.Namespace, sourceEnum.Name, ForcePublic(sourceEnum.Attributes),
                sourceEnum.BaseType?.FullName == "System.ValueType" ? imports.ValueType : imports.Object);
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