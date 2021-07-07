using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower.Passes
{
    public static class Pass10CreateTypedefs
    {
        // These interfaces seem to be runtime-specific and are incompatible as a result
        // TODO: check that methods match between managed/il2cpp versions in general?
        private static readonly HashSet<string> UnsuitableSystemInterfaces = new()
        {
            "System.Runtime.InteropServices._Attribute"
        };

        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            {
                foreach (var type in assemblyContext.OriginalAssembly.MainModule.Types)
                    ProcessType(type, assemblyContext, null);
            }
            
            LogSupport.Trace($"Interface statistics: {context.Statistics.SystemInterfaceCandidates} candidates, of them {context.Statistics.EligibleSystemInterfaces} used");
        }

        private static void ProcessType(TypeDefinition type, AssemblyRewriteContext assemblyContext, TypeDefinition? parentType)
        {
            if (parentType == null)
            {
                if (type.IsInterface)
                {
                    var sameTypeInSystem = assemblyContext.GlobalContext.SystemAssemblies.GetTypeByOriginalTypeAnyAssembly(type);
                    if (sameTypeInSystem != null)
                    {
                        assemblyContext.GlobalContext.Statistics.SystemInterfaceCandidates++;
                        if (IsSystemInterfaceSuitable(type))
                        {
                            assemblyContext.GlobalContext.Statistics.EligibleSystemInterfaces++;

                            var context = new TypeRewriteContext(assemblyContext, type, sameTypeInSystem,
                                TypeRewriteContext.TypeRewriteSemantic.UseSystemInterface);
                            assemblyContext.RegisterTypeRewrite(context);

                            return;
                        }
                    }
                }

                if (type.IsPrimitive || type.FullName == "System.Void" || type.IsEnum)
                {
                    var systemType = assemblyContext.GlobalContext.SystemAssemblies.GetTypeByOriginalTypeOwnAssemblyFirst(type);
                    if (systemType == null && !type.IsEnum)
                        throw new ApplicationException($"System type for primitive type {type} was not found");

                    if (systemType != null && (!type.IsEnum || type.Fields.Count == systemType.Fields.Count))
                    {
                        var context = new TypeRewriteContext(assemblyContext, type, systemType, TypeRewriteContext.TypeRewriteSemantic.UseSystemValueType);
                        assemblyContext.RegisterTypeRewrite(context);

                        return;
                    }
                }
            }

            var convertedTypeName = GetConvertedTypeName(assemblyContext.GlobalContext, type, parentType);
            var newType = new TypeDefinition(convertedTypeName.Namespace ?? type.Namespace.UnSystemify(), convertedTypeName.Name, AdjustAttributes(type.Attributes));

            if (type.IsSealed && type.IsAbstract) // is static
                newType.IsSealed = newType.IsAbstract = true;

            if (type.IsInterface)
                newType.IsInterface = true;

            if (parentType == null)
                assemblyContext.NewAssembly.MainModule.Types.Add(newType);
            else
            {
                parentType.NestedTypes.Add(newType);
                newType.DeclaringType = parentType;
            }
            
            foreach (var typeNestedType in type.NestedTypes) 
                ProcessType(typeNestedType, assemblyContext, newType);

            assemblyContext.RegisterTypeRewrite(new TypeRewriteContext(assemblyContext, type, newType, newType.IsInterface ? TypeRewriteContext.TypeRewriteSemantic.Interface : TypeRewriteContext.TypeRewriteSemantic.Default));
        }

        internal static (string? Namespace, string Name) GetConvertedTypeName(RewriteGlobalContext assemblyContextGlobalContext, TypeDefinition type, TypeDefinition? enclosingType)
        {
            if (assemblyContextGlobalContext.Options.PassthroughNames)
                return (null, type.Name);

            if (type.Name.IsObfuscated(assemblyContextGlobalContext.Options))
            {
                var newNameBase = assemblyContextGlobalContext.RenamedTypes[type];
                var genericParametersCount = type.GenericParameters.Count;
                var renameGroup =
                    assemblyContextGlobalContext.RenameGroups[((object) type.DeclaringType ?? type.Namespace, newNameBase, genericParametersCount)];
                var genericSuffix = genericParametersCount == 0 ? "" : "`" + genericParametersCount;
                var convertedTypeName = newNameBase + (renameGroup.Count == 1 ? "Unique" : renameGroup.IndexOf(type).ToString()) + genericSuffix;

                var fullName = enclosingType == null
                    ? type.Namespace
                    : (enclosingType.GetNamespacePrefix() + "." + enclosingType.Name);

                if (assemblyContextGlobalContext.Options.RenameMap.TryGetValue(fullName + "." + convertedTypeName, out var newName))
                {
                    var lastDotPosition = newName.LastIndexOf(".");
                    if (lastDotPosition >= 0)
                    {
                        var ns = newName.Substring(0, lastDotPosition);
                        var name = newName.Substring(lastDotPosition + 1);
                        return (ns, name);
                    } else 
                        convertedTypeName = newName;
                }

                return (null, convertedTypeName);
            }

            if (type.Name.IsInvalidInSource())
                return (null, type.Name.FilterInvalidInSourceChars());

            return (null, type.Name);
        }

        private static TypeAttributes AdjustAttributes(TypeAttributes typeAttributes)
        {
            typeAttributes |= TypeAttributes.BeforeFieldInit;
            
            var visibility = typeAttributes & TypeAttributes.VisibilityMask;
            if (visibility == 0 || visibility == TypeAttributes.Public)
                return typeAttributes | TypeAttributes.Public;

            return typeAttributes & ~(TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
        }

        private static readonly HashSet<TypeReference> InterfacesUnderConsideration = new HashSet<TypeReference>();

        private static bool IsSystemInterfaceSuitable(TypeDefinition type)
        {
            if (type.HasNestedTypes) 
                return false;

            if (InterfacesUnderConsideration.Contains(type))
                return true;

            if (UnsuitableSystemInterfaces.Contains(type.FullName))
                return false;

            try
            {
                InterfacesUnderConsideration.Add(type);
                
                foreach (var interfaceImplementation in type.Interfaces)
                {
                    if (!IsSystemInterfaceSuitable(interfaceImplementation.InterfaceType.Resolve()))
                        return false;
                }

                foreach (var methodDefinition in type.Methods)
                {
                    if (!IsTypeSuitableForSystemInterfaceReuse(methodDefinition.ReturnType) ||
                        methodDefinition.Parameters.Any(it => !IsTypeSuitableForSystemInterfaceReuse(it.ParameterType)))
                        return false;
                }

                return true;
            }
            finally
            {
                InterfacesUnderConsideration.Remove(type);
            }
        }

        private static bool IsTypeSuitableForSystemInterfaceReuse(TypeReference typeRef)
        {
            if (typeRef is ArrayType)
                return false;
            
            if (typeRef is GenericInstanceType genericInstance)
            {
                var genericBase = genericInstance.ElementType.Resolve();
                if (!genericBase.IsInterface) return false;
                foreach (var parameter in genericInstance.GenericArguments)
                    if (parameter is not GenericParameter && !IsTypeSuitableForSystemInterfaceReuse(parameter.Resolve()))
                        return false;
            }

            if (typeRef is ByReferenceType byRef)
                return IsTypeSuitableForSystemInterfaceReuse(byRef.ElementType);

            if (typeRef.FullName == "System.Object" || typeRef.FullName == "System.Void" || typeRef.IsPrimitive || typeRef is GenericParameter)
                return true;

            var resolved = typeRef.Resolve();
            return resolved.IsInterface && IsSystemInterfaceSuitable(resolved);
        }
    }
}