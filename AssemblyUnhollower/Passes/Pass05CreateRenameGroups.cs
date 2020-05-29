using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Utils;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    public static class Pass05CreateRenameGroups
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            foreach (var originalType in assemblyContext.OriginalAssembly.MainModule.Types)
                ProcessType(context, originalType, false);

            var typesToRemove = context.RenameGroups.Where(it => it.Value.Count > 1).ToList();
            foreach (var keyValuePair in typesToRemove)
            {
                context.RenameGroups.Remove(keyValuePair.Key);
                foreach (var typeDefinition in keyValuePair.Value) 
                    context.RenamedTypes.Remove(typeDefinition);
            }
            
            foreach (var contextRenamedType in context.RenamedTypes)
                context.PreviousRenamedTypes[contextRenamedType.Key] = contextRenamedType.Value;

            foreach (var assemblyContext in context.Assemblies)
            foreach (var originalType in assemblyContext.OriginalAssembly.MainModule.Types)
                ProcessType(context, originalType, true);
        }

        private static void ProcessType(RewriteGlobalContext context, TypeDefinition originalType, bool allowExtraHeuristics)
        {
            foreach (var nestedType in originalType.NestedTypes) 
                ProcessType(context, nestedType, allowExtraHeuristics);

            if (context.RenamedTypes.ContainsKey(originalType)) return;
            
            var unobfuscatedName = GetUnobfuscatedNameBase(context, originalType, allowExtraHeuristics);
            if (unobfuscatedName == null) return;
                    
            context.RenameGroups.GetOrCreate(((object) originalType.DeclaringType ?? originalType.Namespace, unobfuscatedName, originalType.GenericParameters.Count), _ => new List<TypeDefinition>()).Add(originalType);
            context.RenamedTypes[originalType] = unobfuscatedName;
        }

        private static readonly string[] ClassAccessNames = { "Private", "Public", "NPublic", "NPrivate", "NProtected", "NInternal", "NFamAndAssem", "NFamOrAssem" };
        private static string? GetUnobfuscatedNameBase(RewriteGlobalContext context, TypeDefinition typeDefinition, bool allowExtraHeuristics)
        {
            var options = context.Options;
            if (!typeDefinition.Name.IsObfuscated()) return null;

            var inheritanceDepth = 0;
            var firstUnobfuscatedType = typeDefinition.BaseType;
            while (firstUnobfuscatedType != null && firstUnobfuscatedType.Name.IsObfuscated())
            {
                firstUnobfuscatedType = firstUnobfuscatedType.Resolve().BaseType?.Resolve();
                inheritanceDepth++;
            }

            var unobfuscatedInterfacesList = typeDefinition.Interfaces.Select(it => it.InterfaceType).Where(it => !it.Name.IsObfuscated());
            var accessName = ClassAccessNames[(int) (typeDefinition.Attributes & TypeAttributes.VisibilityMask)];

            var classifier = typeDefinition.IsInterface ? "Interface" : (typeDefinition.IsValueType ? "Struct" : "Class");
            var compilerGenertaedString = typeDefinition.Name.StartsWith("<") ? "CompilerGenerated" : "";
            var abstractString = typeDefinition.IsAbstract ? "Abstract" : "";
            var sealedString = typeDefinition.IsSealed ? "Sealed" : "";
            var specialNameString = typeDefinition.IsSpecialName ? "SpecialName" : "";

            var nameBuilder = new StringBuilder();
            nameBuilder.Append(firstUnobfuscatedType?.GenericNameToStrings(context)?.ConcatAll() ?? classifier);
            if (inheritanceDepth > 0)
                nameBuilder.Append(inheritanceDepth);
            nameBuilder.Append(compilerGenertaedString);
            nameBuilder.Append(accessName);
            nameBuilder.Append(abstractString);
            nameBuilder.Append(sealedString);
            nameBuilder.Append(specialNameString);
            foreach (var interfaceRef in unobfuscatedInterfacesList) 
                nameBuilder.Append(interfaceRef.GenericNameToStrings(context).ConcatAll());

            var uniqContext = new UniquificationContext(options);
            foreach (var fieldDef in typeDefinition.Fields)
            {
                if (!typeDefinition.IsEnum)
                    uniqContext.Push(fieldDef.FieldType.GenericNameToStrings(context));
                
                uniqContext.Push(fieldDef.Name);
                
                if (uniqContext.CheckFull()) break;
            }

            if (typeDefinition.IsEnum) 
                uniqContext.Push(typeDefinition.Fields.Count + "v");

            foreach (var propertyDef in typeDefinition.Properties)
            {
                uniqContext.Push(propertyDef.PropertyType.GenericNameToStrings(context));
                uniqContext.Push(propertyDef.Name);

                if (uniqContext.CheckFull()) break;
            }

            if (firstUnobfuscatedType?.Name == "MulticastDelegate")
            {
                var invokeMethod = typeDefinition.Methods.SingleOrDefault(it => it.Name == "Invoke");
                if (invokeMethod != null)
                {
                    uniqContext.Push(invokeMethod.ReturnType.GenericNameToStrings(context));

                    foreach (var parameterDef in invokeMethod.Parameters)
                    {
                        uniqContext.Push(parameterDef.ParameterType.GenericNameToStrings(context));
                        if (uniqContext.CheckFull()) break;
                    }
                }
            }
            
            if (typeDefinition.IsInterface || allowExtraHeuristics) // method order on non-interface types appears to be unstable
                foreach (var methodDefinition in typeDefinition.Methods)
                {
                    uniqContext.Push(methodDefinition.Name);
                    uniqContext.Push(methodDefinition.ReturnType.GenericNameToStrings(context));

                    foreach (var parameter in methodDefinition.Parameters)
                    {
                        uniqContext.Push(parameter.Name);
                        uniqContext.Push(parameter.ParameterType.GenericNameToStrings(context));

                        if (uniqContext.CheckFull()) break;
                    }

                    if (uniqContext.CheckFull()) break;
                }

            nameBuilder.Append(uniqContext.GetTop());

            return nameBuilder.ToString();
        }

        private static string ConcatAll(this List<string> strings) => string.Concat(strings);

        private static string NameOrRename(this TypeReference typeRef, RewriteGlobalContext context)
        {
            var resolved = typeRef.Resolve();
            if (resolved != null && context.PreviousRenamedTypes.TryGetValue(resolved, out var rename))
                return (rename.StableHash() % (ulong) Math.Pow(10, context.Options.TypeDeobfuscationCharsPerUniquifier)).ToString();
            
            return typeRef.Name;
        }

        private static List<string> GenericNameToStrings(this TypeReference typeRef, RewriteGlobalContext context)
        {
            if (typeRef is ArrayType arrayType)
                return arrayType.ElementType.GenericNameToStrings(context);

            if (typeRef is GenericInstanceType genericInstance)
            {
                var baseTypeName = genericInstance.GetElementType().NameOrRename(context);
                var indexOfBacktick = baseTypeName.IndexOf('`');
                if (indexOfBacktick >= 0)
                    baseTypeName = baseTypeName.Substring(0, indexOfBacktick);

                var entries = new List<string>();
                
                entries.Add(baseTypeName);
                entries.Add(genericInstance.GenericArguments.Count.ToString());
                foreach (var genericArgument in genericInstance.GenericArguments) 
                    entries.AddRange(genericArgument.GenericNameToStrings(context));
                return entries;
            }

            if (typeRef.NameOrRename(context).IsObfuscated())
                return new List<string> {"Obf"};

            return new List<string> {typeRef.NameOrRename(context)};
        }
    }
}