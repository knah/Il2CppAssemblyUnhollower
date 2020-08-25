using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass10CreateTypedefs
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var type in assemblyContext.OriginalAssembly.MainModule.Types)
                    ProcessType(type, assemblyContext, null);
            }
        }

        private static void ProcessType(TypeDefinition type, AssemblyRewriteContext assemblyContext, TypeDefinition? parentType)
        {
            var newType = new TypeDefinition(type.Namespace.UnSystemify(), GetConvertedTypeName(assemblyContext.GlobalContext, type), AdjustAttributes(type.Attributes));
            
            if(parentType == null)
                assemblyContext.NewAssembly.MainModule.Types.Add(newType);
            else
            {
                parentType.NestedTypes.Add(newType);
                newType.DeclaringType = parentType;
            }
            
            foreach (var typeNestedType in type.NestedTypes) 
                ProcessType(typeNestedType, assemblyContext, newType);

            assemblyContext.RegisterTypeRewrite(new TypeRewriteContext(assemblyContext, type, newType));
        }

        private static string GetConvertedTypeName(RewriteGlobalContext assemblyContextGlobalContext, TypeDefinition type)
        {
            if (type.Name.IsInvalidInSource())
            {
                var newNameBase = assemblyContextGlobalContext.RenamedTypes[type];
                var genericParametersCount = type.GenericParameters.Count;
                var renameGroup =
                    assemblyContextGlobalContext.RenameGroups[((object) type.DeclaringType ?? type.Namespace, newNameBase, genericParametersCount)];
                var genericSuffix = genericParametersCount == 0 ? "" : "`" + genericParametersCount;
                return newNameBase + (renameGroup.Count == 1 ? "Unique" : renameGroup.IndexOf(type).ToString()) + genericSuffix;
            }

            return type.Name;
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