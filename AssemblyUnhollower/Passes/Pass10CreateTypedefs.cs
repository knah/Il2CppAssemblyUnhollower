using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;
using System;

namespace AssemblyUnhollower.Passes
{
    public static class Pass10CreateTypedefs
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var type in assemblyContext.OriginalAssembly.MainModule.Types)
                    ProcessType(type, assemblyContext, null, null);
            }
        }

        private static void ProcessType(TypeDefinition type, AssemblyRewriteContext assemblyContext, TypeDefinition? parentType, string? parentName)
        {
            var convertedTypeName = GetConvertedTypeName(assemblyContext.GlobalContext, type, parentName);
            var newType = new TypeDefinition(convertedTypeName.Namespace ?? type.Namespace.UnSystemify(), convertedTypeName.Name, AdjustAttributes(type.Attributes));

            if (type.IsSealed && type.IsAbstract) // is static
            {
                newType.IsSealed = newType.IsAbstract = true;
            }
            
            if(parentType == null)
                assemblyContext.NewAssembly.MainModule.Types.Add(newType);
            else
            {
                parentType.NestedTypes.Add(newType);
                newType.DeclaringType = parentType;
            }
            
            foreach (var typeNestedType in type.NestedTypes) 
                ProcessType(typeNestedType, assemblyContext, newType, convertedTypeName.MapName);

            assemblyContext.RegisterTypeRewrite(new TypeRewriteContext(assemblyContext, type, newType, convertedTypeName.MapName));
        }

        internal static (string? Namespace, string Name, string MapName) GetConvertedTypeName(RewriteGlobalContext assemblyContextGlobalContext, TypeDefinition type, string? parentName)
        {
            if (assemblyContextGlobalContext.Options.PassthroughNames)
                return (null, type.Name, type.Name);

            if (type.Name.IsObfuscated(assemblyContextGlobalContext.Options))
            {
                var newNameBase = assemblyContextGlobalContext.RenamedTypes[type];
                var genericParametersCount = type.GenericParameters.Count;
                var renameGroup =
                    assemblyContextGlobalContext.RenameGroups[((object) type.DeclaringType ?? type.Namespace, newNameBase, genericParametersCount)];
                var genericSuffix = genericParametersCount == 0 ? "" : "`" + genericParametersCount;
                var convertedTypeName = newNameBase + (renameGroup.Count == 1 ? "Unique" : renameGroup.IndexOf(type).ToString()) + genericSuffix;

                var fullName = parentName == null
                    ? type.Namespace
                    : parentName;

                var mapName = fullName + "." + convertedTypeName;

                if (assemblyContextGlobalContext.Options.RenameMap.TryGetValue(mapName, out var newName))
                {
                    var lastDotPosition = newName.LastIndexOf(".");
                    if (lastDotPosition >= 0)
                    {
                        var ns = newName.Substring(0, lastDotPosition);
                        var name = newName.Substring(lastDotPosition + 1);
                        return (ns, name, mapName);
                    } else 
                        convertedTypeName = newName;
                }

                return (null, convertedTypeName, mapName);
            }

            if (type.Name.IsInvalidInSource())
                return (null, type.Name.FilterInvalidInSourceChars(), type.Name.FilterInvalidInSourceChars());

            return (null, type.Name, type.Name);
        }

        private static TypeAttributes AdjustAttributes(TypeAttributes typeAttributes)
        {
            typeAttributes |= TypeAttributes.BeforeFieldInit;
            typeAttributes &= ~(TypeAttributes.Abstract | TypeAttributes.Interface);
            
            var visibility = typeAttributes & TypeAttributes.VisibilityMask;
            if (visibility == 0 || visibility == TypeAttributes.Public)
                return typeAttributes | TypeAttributes.Public;

            return typeAttributes & ~(TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
        }
    }
}