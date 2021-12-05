using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;

namespace AssemblyUnhollower.Passes
{
    /// <summary>
    /// Implicit overrides might get broken by deobfuscation renaming, and sometimes CPP2IL misses explicit overrides
    /// This pass attempts to restore them where needed
    /// </summary>
    public static class Pass20FixupMissingInterfaceMethods
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            var typesByInheritanceDepth = new Dictionary<int, List<TypeRewriteContext>>();

            foreach (var assemblyRewriteContext in context.Assemblies)
            foreach (var typeRewriteContext in assemblyRewriteContext.Types)
            {
                if (typeRewriteContext.RewriteSemantic != TypeRewriteContext.TypeRewriteSemantic.Default || typeRewriteContext.NewType.IsEnum) continue;
                
                var depth = 0;
                var baseType = typeRewriteContext.NewType.BaseType?.Resolve();
                while (baseType != null && baseType.Namespace != nameof(UnhollowerBaseLib))
                {
                    depth++;
                    baseType = baseType.BaseType?.Resolve();
                }
                typesByInheritanceDepth.GetOrCreate(depth, _ => new()).Add(typeRewriteContext);
            }

            foreach (var depth in typesByInheritanceDepth.Keys.OrderBy(it => it))
            foreach (var typeRewriteContext in typesByInheritanceDepth[depth])
            {
                if (typeRewriteContext.RewriteSemantic != TypeRewriteContext.TypeRewriteSemantic.Default || typeRewriteContext.NewType.IsEnum) continue;
                
                var allInterfaceMethods = GatherInterfaceMethods(typeRewriteContext);
                
                RemoveExplicitlyOverriddenMethods(typeRewriteContext.NewType, allInterfaceMethods);
                RemoveImplicitlyOverriddenMethods(typeRewriteContext.NewType, allInterfaceMethods);

                FixupObfuscatedInterfaceMethods(typeRewriteContext, allInterfaceMethods);

                if (allInterfaceMethods.Count > 0)
                {
                    Console.WriteLine($"There are unimplemented interface methods on type {typeRewriteContext.NewType.FullName}");
                }
            }
        }

        private static void FixupObfuscatedInterfaceMethods(TypeRewriteContext typeRewriteContext, HashSet<MethodReference> allInterfaceMethods)
        {
            var interfaceMethodsBinned = new Dictionary<(string Name, int parameterCount, int genericParameterCount), List<MethodReference>>();

            foreach (var interfaceMethod in allInterfaceMethods)
            {
                var interfaceRewriteContext = typeRewriteContext.AssemblyContext.GlobalContext.GetContextForNewType(interfaceMethod.DeclaringType.Resolve());
                var interfaceMethodContext = interfaceRewriteContext.GetMethodByNewMethod(interfaceMethod.Resolve());
                interfaceMethodsBinned
                    .GetOrCreate(
                        (interfaceMethodContext.OriginalMethod.Name, interfaceMethod.Parameters.Count,
                            interfaceMethod.GenericParameters.Count), _ => new()).Add(interfaceMethod);
            }
            
            foreach (var methodRewriteContext in typeRewriteContext.Methods)
            {
                var binKey = (methodRewriteContext.OriginalMethod.Name, methodRewriteContext.NewMethod.Parameters.Count,
                    methodRewriteContext.NewMethod.GenericParameters.Count);

                if (interfaceMethodsBinned.TryGetValue(binKey, out var bin))
                {
                    if (bin.Count == 0)
                    {
                        Console.WriteLine($"Empty bin for method {typeRewriteContext.NewType.FullName}::{methodRewriteContext.NewMethod.Name}, need more granular bins");
                        continue;
                    }
                    
                    if (bin.Count > 1)
                        Console.WriteLine($"Thick bin for method {typeRewriteContext.NewType.FullName}::{methodRewriteContext.NewMethod.Name}, need more granular bins");
                    
                    methodRewriteContext.NewMethod.Overrides.Add(methodRewriteContext.DeclaringType.AssemblyContext.NewAssembly.MainModule.ImportReference(bin[0]));
                    allInterfaceMethods.Remove(bin[0]);
                    bin.RemoveAt(0); // todo: handle generic interfaces better
                }
            }
        }

        private static void RemoveExplicitlyOverriddenMethods(TypeDefinition? type, HashSet<MethodReference> interfaceMethods)
        {
            if (type == null || type.Namespace == nameof(UnhollowerBaseLib)) return;
            
            foreach (var methodDefinition in type.Methods)
            foreach (var methodDefinitionOverride in methodDefinition.Overrides)
                interfaceMethods.Remove(methodDefinitionOverride.Resolve());
            
            RemoveExplicitlyOverriddenMethods(type.BaseType?.Resolve(), interfaceMethods);
        }

        private static void RemoveImplicitlyOverriddenMethods(TypeDefinition? type, HashSet<MethodReference> interfaceMethods)
        {
            if (type == null || type.Namespace == nameof(UnhollowerBaseLib)) return;

            var interfaceMethodsBinned = new Dictionary<(string Name, int parameterCount, int genericParameterCount), List<MethodReference>>();
            
            foreach (var interfaceMethod in interfaceMethods)
                interfaceMethodsBinned
                    .GetOrCreate(
                        (interfaceMethod.Name, interfaceMethod.Parameters.Count,
                            interfaceMethod.GenericParameters.Count), _ => new()).Add(interfaceMethod);
            
            foreach (var methodDefinition in type.Methods)
            {
                var binKey = (methodDefinition.Name, methodDefinition.Parameters.Count,
                    methodDefinition.GenericParameters.Count);
                if (interfaceMethodsBinned.TryGetValue(binKey, out var bin))
                {
                    if (bin.Count > 1)
                        Console.WriteLine($"Dropping thick bin: {type.Name}/{binKey.Name}");

                    foreach (var binElement in bin) 
                        interfaceMethods.Remove(binElement); // todo: filter by parameter types?

                    bin.Clear();
                }
            }

            RemoveImplicitlyOverriddenMethods(type.BaseType?.Resolve(), interfaceMethods);
        }

        private static HashSet<MethodReference> GatherInterfaceMethods(TypeRewriteContext type)
        {
            var result = new HashSet<MethodReference>();

            GatherInterfaceMethods(type, result);
            
            return result;
        }

        private static void GatherInterfaceMethods(TypeRewriteContext type, HashSet<MethodReference> result) => GatherInterfaceMethods(type.NewType, result);

        private static void GatherInterfaceMethods(TypeDefinition? type, HashSet<MethodReference> result)
        {
            if (type == null || type.Namespace == nameof(UnhollowerBaseLib)) return;
            
            foreach (var newTypeInterface in type.Interfaces)
            {
                var interfaceType = newTypeInterface.InterfaceType.Resolve();
                foreach (var interfaceTypeMethod in interfaceType.Methods) 
                    result.Add(interfaceTypeMethod); // todo: apply generic substitution in the interface?

                GatherInterfaceMethods(interfaceType, result);
            }

            GatherInterfaceMethods(type.BaseType?.Resolve(), result);
        }
    }
}