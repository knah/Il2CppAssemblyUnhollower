using System.IO;
using System.Linq;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Utils;
using Mono.Cecil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower.Passes
{
    public static class Pass80UnstripMethods
    {
        public static void DoPass(RewriteGlobalContext context)
        {
            var unityAssemblyFiles = Directory.EnumerateFiles(context.Options.UnityBaseLibsDir, "*.dll");
            var loadedAssemblies = unityAssemblyFiles.Select(it =>
                AssemblyDefinition.ReadAssembly(it, new ReaderParameters(ReadingMode.Deferred))).ToList();

            int methodsUnstripped = 0;
            int methodsIgnored = 0;
            
            foreach (var unityAssembly in loadedAssemblies)
            {
                var processedAssembly = context.TryGetAssemblyByName(unityAssembly.Name.Name);
                if (processedAssembly == null) continue;
                var imports = processedAssembly.Imports;
                
                foreach (var unityType in unityAssembly.MainModule.Types)
                {
                    var processedType = processedAssembly.TryGetTypeByName(unityType.FullName);
                    if (processedType == null) continue;
                    
                    foreach (var unityMethod in unityType.Methods)
                    {
                        if (unityMethod.Name == ".cctor" || unityMethod.Name == ".ctor") continue;
                        
                        var processedMethod = processedType.TryGetMethodByName(unityMethod.Name);
                        if (processedMethod != null) continue;

                        var returnType = ResolveTypeInNewAssemblies(context, unityMethod.ReturnType, imports);
                        if (returnType == null)
                        {
                            LogSupport.Trace($"Method {unityMethod} has unsupported return type {unityMethod.ReturnType}");
                            methodsIgnored++;
                            continue;
                        }
                        
                        var newMethod = new MethodDefinition(unityMethod.Name, unityMethod.Attributes & ~MethodAttributes.MemberAccessMask | MethodAttributes.Public, returnType);
                        var hadBadParameter = false;
                        foreach (var unityMethodParameter in unityMethod.Parameters)
                        {
                            var convertedType = ResolveTypeInNewAssemblies(context, unityMethodParameter.ParameterType, imports);
                            if (convertedType == null)
                            {
                                hadBadParameter = true;
                                LogSupport.Trace($"Method {unityMethod} has unsupported parameter type {unityMethodParameter.ParameterType}");
                                break;
                            }

                            newMethod.Parameters.Add(new ParameterDefinition(unityMethodParameter.Name, unityMethodParameter.Attributes, convertedType));
                        }

                        if (hadBadParameter)
                        {
                            methodsIgnored++;
                            continue;
                        }
                        
                        foreach (var unityMethodGenericParameter in unityMethod.GenericParameters)
                        {
                            var newParameter = new GenericParameter(unityMethodGenericParameter.Name, newMethod);
                            newParameter.Attributes = unityMethodGenericParameter.Attributes;
                            foreach (var genericParameterConstraint in unityMethodGenericParameter.Constraints)
                            {
                                if (genericParameterConstraint.ConstraintType.FullName == "System.ValueType") continue;
                                if (genericParameterConstraint.ConstraintType.Resolve().IsInterface) continue;

                                var newType = ResolveTypeInNewAssemblies(context, genericParameterConstraint.ConstraintType, imports);
                                if (newType != null)
                                    newParameter.Constraints.Add(new GenericParameterConstraint(newType));
                            }
                            
                            newMethod.GenericParameters.Add(newParameter);
                        }

                        if ((unityMethod.ImplAttributes & MethodImplAttributes.InternalCall) != 0)
                        {
                            var delegateType = UnstripGenerator.CreateDelegateTypeForICallMethod(unityMethod, newMethod, imports);
                            processedType.NewType.NestedTypes.Add(delegateType);
                            delegateType.DeclaringType = processedType.NewType;
                        
                            processedType.NewType.Methods.Add(newMethod);

                            var delegateField = UnstripGenerator.GenerateStaticCtorSuffix(processedType.NewType, delegateType, unityMethod, imports);
                            UnstripGenerator.GenerateInvokerMethodBody(newMethod, delegateField, delegateType, processedType, imports);
                        }
                        else
                        {
                            Pass81FillUnstrippedMethodBodies.PushMethod(unityMethod, newMethod, processedType, imports);
                            processedType.NewType.Methods.Add(newMethod);
                        }

                        if (unityMethod.IsGetter)
                            GetOrCreateProperty(unityMethod, newMethod).GetMethod = newMethod;
                        else if(unityMethod.IsSetter)
                            GetOrCreateProperty(unityMethod, newMethod).SetMethod = newMethod;

                        methodsUnstripped++;
                    }
                }
            }
            
            LogSupport.Info(""); // finish the progress line
            LogSupport.Info($"{methodsUnstripped} methods restored");
            LogSupport.Info($"{methodsIgnored} methods failed to restore");
        }

        private static PropertyDefinition GetOrCreateProperty(MethodDefinition unityMethod, MethodDefinition newMethod)
        {
            var unityProperty = unityMethod.DeclaringType.Properties.Single(it => it.SetMethod == unityMethod || it.GetMethod == unityMethod);
            var newProperty = newMethod.DeclaringType.Properties.SingleOrDefault(it => it.Name == unityProperty.Name);
            if (newProperty == null)
            {
                newProperty = new PropertyDefinition(unityProperty.Name, PropertyAttributes.None, unityMethod.IsGetter ? newMethod.ReturnType : newMethod.Parameters.Last().ParameterType);
                newMethod.DeclaringType.Properties.Add(newProperty);
            }

            return newProperty;
        } 

        internal static TypeReference? ResolveTypeInNewAssemblies(RewriteGlobalContext context, TypeReference unityType,
            AssemblyKnownImports imports)
        {
            var resolved = ResolveTypeInNewAssembliesRaw(context, unityType, imports);
            return resolved != null ? imports.Module.ImportReference(resolved) : null;
        }

        internal static TypeReference? ResolveTypeInNewAssembliesRaw(RewriteGlobalContext context, TypeReference unityType, AssemblyKnownImports imports)
        {
            if (unityType is ByReferenceType)
            {
                var resolvedElementType = ResolveTypeInNewAssemblies(context, unityType.GetElementType(), imports);
                return resolvedElementType == null ? null : new ByReferenceType(resolvedElementType);
            }

            if (unityType is GenericParameter)
                return null;

            if (unityType is ArrayType arrayType)
            {
                if (arrayType.Rank != 1) return null;
                var resolvedElementType = ResolveTypeInNewAssemblies(context, unityType.GetElementType(), imports);
                if (resolvedElementType == null) return null;
                if (resolvedElementType.FullName == "System.String")
                    return imports.Il2CppStringArray;
                var genericBase = resolvedElementType.IsValueType
                    ? imports.Il2CppStructArray
                    : imports.Il2CppReferenceArray;
                return new GenericInstanceType(genericBase) { GenericArguments = { resolvedElementType }};
            }

            if (unityType.DeclaringType != null)
            {
                var enclosingResolvedType = ResolveTypeInNewAssembliesRaw(context, unityType.DeclaringType, imports);
                if (enclosingResolvedType == null) return null;
                var resolvedNestedType = enclosingResolvedType.Resolve().NestedTypes.FirstOrDefault(it => it.Name == unityType.Name);
                if (resolvedNestedType == null) return null;
                return resolvedNestedType;
            }

            if (unityType is PointerType)
            {
                var resolvedElementType = ResolveTypeInNewAssemblies(context, unityType.GetElementType(), imports);
                return resolvedElementType == null ? null : new PointerType(resolvedElementType);
            }

            if (unityType is GenericInstanceType genericInstance)
            {
                var baseRef = ResolveTypeInNewAssemblies(context, genericInstance.ElementType, imports);
                if (baseRef == null) return null;
                var newInstance = new GenericInstanceType(baseRef);
                foreach (var unityGenericArgument in genericInstance.GenericArguments)
                {
                    var resolvedArgument = ResolveTypeInNewAssemblies(context, unityGenericArgument, imports);
                    if (resolvedArgument == null) return null;
                    newInstance.GenericArguments.Add(resolvedArgument);
                }

                return newInstance;
            }

            var targetAssemblyName = unityType.Scope.Name;
            if (targetAssemblyName.EndsWith(".dll"))
                targetAssemblyName = targetAssemblyName.Substring(0, targetAssemblyName.Length - 4);
            if (targetAssemblyName == "mscorlib" && (unityType.IsValueType || unityType.FullName == "System.String" || unityType.FullName == "System.Void"))
                return TargetTypeSystemHandler.Type.Module.GetType(unityType.FullName);

            var targetAssembly = context.TryGetAssemblyByName(targetAssemblyName);
            var newType = targetAssembly?.TryGetTypeByName(unityType.FullName)?.NewType;
            if (newType == null) return null;
            
            return newType;
        }
    }
}