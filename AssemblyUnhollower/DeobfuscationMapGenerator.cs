using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using AssemblyUnhollower.MetadataAccess;
using AssemblyUnhollower.Passes;
using Mono.Cecil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower
{
    public static class DeobfuscationMapGenerator
    {
        public static void GenerateDeobfuscationMap(UnhollowerOptions options)
        {
            if (string.IsNullOrEmpty(options.SourceDir))
            {
                Console.WriteLine("No input dir specified; use -h for help");
                return;
            }
            
            if (string.IsNullOrEmpty(options.OutputDir))
            {
                Console.WriteLine("No target dir specified; use -h for help");
                return;
            }
            if (string.IsNullOrEmpty(options.DeobfuscationNewAssembliesPath))
            {
                Console.WriteLine("No obfuscated assembly path specified; use -h for help");
                return;
            }

            if (!Directory.Exists(options.OutputDir))
                Directory.CreateDirectory(options.OutputDir);

            RewriteGlobalContext rewriteContext;
            IIl2CppMetadataAccess inputAssemblies;
            IIl2CppMetadataAccess systemAssemblies;
            using (new TimingCookie("Reading assemblies"))
                inputAssemblies = new CecilMetadataAccess(Directory.EnumerateFiles(options.DeobfuscationNewAssembliesPath, "*.dll"));
            
            using (new TimingCookie("Reading system assemblies"))
            {
                if (!string.IsNullOrEmpty(options.SystemLibrariesPath)) 
                    systemAssemblies = new CecilMetadataAccess(Directory.EnumerateFiles(options.SystemLibrariesPath, "*.dll")
                        .Where(it => Path.GetFileName(it).StartsWith("System.") || Path.GetFileName(it) == "mscorlib.dll" || Path.GetFileName(it) == "netstandard.dll"));
                else
                    systemAssemblies = new CecilMetadataAccess(new[] {options.MscorlibPath});

            }
            
            using(new TimingCookie("Creating rewrite assemblies"))
                rewriteContext = new RewriteGlobalContext(options, inputAssemblies, systemAssemblies, NullMetadataAccess.Instance);
            using(new TimingCookie("Computing renames"))
                Pass05CreateRenameGroups.DoPass(rewriteContext);
            using(new TimingCookie("Creating typedefs"))
                Pass10CreateTypedefs.DoPass(rewriteContext);
            using(new TimingCookie("Computing struct blittability"))
                Pass11ComputeTypeSpecifics.DoPass(rewriteContext);
            using(new TimingCookie("Filling typedefs"))
                Pass12FillTypedefs.DoPass(rewriteContext);
            using(new TimingCookie("Filling generic constraints"))
                Pass13FillGenericConstraints.DoPass(rewriteContext);
            using(new TimingCookie("Creating members"))
                Pass15GenerateMemberContexts.DoPass(rewriteContext);


            RewriteGlobalContext cleanContext;
            IIl2CppMetadataAccess cleanAssemblies;
            using (new TimingCookie("Reading clean assemblies"))
                cleanAssemblies = new CecilMetadataAccess(Directory.EnumerateFiles(options.SourceDir, "*.dll"));
            
            using(new TimingCookie("Creating clean rewrite assemblies"))
                cleanContext = new RewriteGlobalContext(options, cleanAssemblies, systemAssemblies, NullMetadataAccess.Instance);
            using(new TimingCookie("Computing clean assembly renames"))
                Pass05CreateRenameGroups.DoPass(cleanContext);
            using(new TimingCookie("Creating clean assembly typedefs"))
                Pass10CreateTypedefs.DoPass(cleanContext);


            var usedNames = new Dictionary<TypeDefinition, (string OldName, int Penalty, bool ForceNs)>();
            
            using var fileOutput = new FileStream(options.OutputDir + Path.DirectorySeparatorChar + "RenameMap.csv.gz", FileMode.Create, FileAccess.Write);
            using var gzipStream = new GZipStream(fileOutput, CompressionLevel.Optimal, true);
            using var writer = new StreamWriter(gzipStream, Encoding.UTF8, 65536, true);

            void DoEnum(TypeRewriteContext obfuscatedType, TypeRewriteContext cleanType)
            {
                foreach (var originalTypeField in obfuscatedType.OriginalType.Fields)
                {
                    if (!originalTypeField.Name.IsObfuscated(obfuscatedType.AssemblyContext.GlobalContext.Options)) continue;
                    var matchedField = cleanType.OriginalType.Fields[obfuscatedType.OriginalType.Fields.IndexOf(originalTypeField)];


                    string maybeWithDot = obfuscatedType.NewType.GetNamespacePrefix() + ".";
                    //if (maybeWithDot.IndexOf('.') == 0) maybeWithDot = maybeWithDot.Substring(1);
                    writer.WriteLine(obfuscatedType.NewType.GetNamespacePrefix() + obfuscatedType.NewType.Name + "::" + Pass22GenerateEnums.GetUnmangledName(originalTypeField) + ";" + matchedField.Name + ";0");
                }
            }
            
            foreach (var assemblyContext in rewriteContext.Assemblies)
            {
                if (options.DeobfuscationGenerationAssemblies.Count > 0 &&
                    !options.DeobfuscationGenerationAssemblies.Contains(assemblyContext.NewAssembly.Name.Name))
                    continue;

                var cleanAssembly = cleanContext.GetAssemblyByName(assemblyContext.OriginalAssembly.Name.Name);

                void DoType(TypeRewriteContext typeContext, TypeRewriteContext? enclosingType)
                {
                    if(cleanAssembly.TryGetTypeByName(typeContext.NewType.Name) != null) return;

                    var cleanType = FindBestMatchType(typeContext, cleanAssembly, enclosingType);
                    if (cleanType.Item1 == null) return;
                    
                    if (!usedNames.TryGetValue(cleanType.Item1.NewType, out var existing) || existing.Penalty < cleanType.Item2)
                    {
                        string maybeWithDot = typeContext.NewType.GetNamespacePrefix() + ".";
                        //if (maybeWithDot.IndexOf('.') == 0) maybeWithDot = maybeWithDot.Substring(1);
                        usedNames[cleanType.Item1.NewType] = (maybeWithDot + typeContext.NewType.Name, cleanType.Item2, typeContext.OriginalType.Namespace != cleanType.Item1.OriginalType.Namespace);
                    } else 
                        return;

                    if (typeContext.OriginalType.IsEnum) 
                        DoEnum(typeContext, cleanType.Item1);

                    foreach (var originalTypeNestedType in typeContext.OriginalType.NestedTypes)
                        DoType(typeContext.AssemblyContext.GetContextForOriginalType(originalTypeNestedType), cleanType.Item1);
                }
                
                foreach (var typeContext in assemblyContext.Types)
                {
                    if(typeContext.NewType.DeclaringType != null) continue;
                    
                    DoType(typeContext, null);
                }
            }
            
            
            foreach (var keyValuePair in usedNames)
                writer.WriteLine(keyValuePair.Value.Item1 + ";" + (keyValuePair.Value.ForceNs ? keyValuePair.Key.Namespace + "." : "") + keyValuePair.Key.Name + ";" + keyValuePair.Value.Item2);

            LogSupport.Info("Done!");

            rewriteContext.Dispose();
        }

        private static (TypeRewriteContext?, int) FindBestMatchType(TypeRewriteContext obfType, AssemblyRewriteContext cleanAssembly, TypeRewriteContext? enclosingCleanType)
        {
            var inheritanceDepthOfOriginal = 0;
            var currentBase = obfType.OriginalType.BaseType;
            while (true)
            {
                if (currentBase == null) break;
                var currentBaseContext = obfType.AssemblyContext.GlobalContext.TryGetNewTypeForOriginal(currentBase.Resolve());
                if (currentBaseContext == null || !currentBaseContext.OriginalNameWasObfuscated) break;

                inheritanceDepthOfOriginal++;
                currentBase = currentBaseContext.OriginalType.BaseType;
            }

            var bestPenalty = int.MinValue;
            TypeRewriteContext? bestMatch = null;

            var source = enclosingCleanType?.OriginalType.NestedTypes.Select(it => cleanAssembly.GlobalContext.GetNewTypeForOriginal(it)) ??
                         cleanAssembly.Types.Where(it => it.NewType.DeclaringType == null); 
            
            foreach (var candidateCleanType in source)
            {
                if(obfType.OriginalType.HasMethods != candidateCleanType.OriginalType.HasMethods)
                    continue;
                
                if(obfType.OriginalType.HasFields != candidateCleanType.OriginalType.HasFields)
                    continue;
                
                if (obfType.OriginalType.IsEnum)
                    if (obfType.OriginalType.Fields.Count != candidateCleanType.OriginalType.Fields.Count)
                        continue;
                
                int currentPenalty = 0;
                
                var tryBase = candidateCleanType.OriginalType.BaseType;
                var actualBaseDepth = 0;
                while (tryBase != null)
                {
                    if (tryBase?.Name == currentBase?.Name && tryBase?.Namespace == currentBase?.Namespace)
                        break;
                    
                    tryBase = tryBase?.Resolve().BaseType;
                    actualBaseDepth++;
                }
                
                if (tryBase == null && currentBase != null)
                    continue;

                var baseDepthDifference = Math.Abs(actualBaseDepth - inheritanceDepthOfOriginal);
                if(baseDepthDifference > 1) continue; // heuristic optimization
                currentPenalty -= baseDepthDifference * 50;

                currentPenalty -= Math.Abs(candidateCleanType.OriginalType.Fields.Count - obfType.OriginalType.Fields.Count) * 5;

                currentPenalty -= Math.Abs(obfType.OriginalType.NestedTypes.Count - candidateCleanType.OriginalType.NestedTypes.Count) * 10;
                
                currentPenalty -= Math.Abs(obfType.OriginalType.Properties.Count - candidateCleanType.OriginalType.Properties.Count) * 5;
                
                currentPenalty -= Math.Abs(obfType.OriginalType.Interfaces.Count - candidateCleanType.OriginalType.Interfaces.Count) * 35;

                var options = obfType.AssemblyContext.GlobalContext.Options;

                foreach (var obfuscatedField in obfType.OriginalType.Fields)
                {
                    if (obfuscatedField.Name.IsObfuscated(options))
                    {
                        
                        var bestFieldScore = candidateCleanType.OriginalType.Fields.Max(it => TypeMatchWeight(obfuscatedField.FieldType, it.FieldType, options));
                        currentPenalty += bestFieldScore * (bestFieldScore < 0 ? 10 : 2);
                        continue;
                    }
                    
                    if (candidateCleanType.OriginalType.Fields.Any(it => it.Name == obfuscatedField.Name))
                        currentPenalty += 10;
                }
                
                foreach (var obfuscatedMethod in obfType.OriginalType.Methods)
                {
                    if (obfuscatedMethod.Name.Contains(".ctor")) continue;
                    
                    if (obfuscatedMethod.Name.IsObfuscated(options))
                    {
                        var bestMethodScore = candidateCleanType.OriginalType.Methods.Max(it => MethodSignatureMatchWeight(obfuscatedMethod, it, options));
                        currentPenalty += bestMethodScore * (bestMethodScore < 0 ? 10 : 1);
                        
                        continue;
                    }

                    if (candidateCleanType.OriginalType.Methods.Any(it => it.Name == obfuscatedMethod.Name))
                        currentPenalty += obfuscatedMethod.Name.Length / 10 * 5 + 1;
                }

                if (currentPenalty == bestPenalty)
                {
                    bestMatch = null;
                } else if (currentPenalty > bestPenalty)
                {
                    bestPenalty = currentPenalty;
                    bestMatch = candidateCleanType;
                }
            }

            // if (bestPenalty < -100)
                // bestMatch = null;

            return (bestMatch, bestPenalty);
        }

        private static int TypeMatchWeight(TypeReference a, TypeReference b, UnhollowerOptions options)
        {
            if (a.GetType() != b.GetType())
                return -1;

            var runningSum = 0;

            void Accumulate(int i)
            {
                if (i < 0 || runningSum < 0)
                    runningSum = -1;
                else
                    runningSum += i;
            }
            
            switch (a)
            {
                case ArrayType arr:
                    if (!(b is ArrayType brr))
                        return -1;
                    return TypeMatchWeight(arr.ElementType, brr.ElementType, options) * 5;
                case ByReferenceType abr:
                    if (!(b is ByReferenceType bbr))
                        return -1;
                    return TypeMatchWeight(abr.ElementType, bbr.ElementType, options) * 5;
                case GenericInstanceType agi:
                    if (!(b is GenericInstanceType bgi))
                        return -1;
                    if (agi.GenericArguments.Count != bgi.GenericArguments.Count) return -1;
                    Accumulate(TypeMatchWeight(agi.ElementType, bgi.ElementType, options));
                    for (var i = 0; i < agi.GenericArguments.Count; i++)
                        Accumulate(TypeMatchWeight(agi.GenericArguments[i], bgi.GenericArguments[i], options));
                    return runningSum * 5;
                case GenericParameter:
                    if (!(b is GenericParameter))
                        return -1;
                    return 5;
                default:
                    if (a.IsNested)
                    {
                        if (!b.IsNested)
                            return -1;
                        
                        if (a.Name.IsObfuscated(options))
                            return 0;
                        
                        var declMatch = TypeMatchWeight(a.DeclaringType, b.DeclaringType, options);
                        if (declMatch == -1 || a.Name != b.Name)
                            return -1;

                        return 1;
                    }
                    if (a.Name.IsObfuscated(options))
                        return 0;
                    return a.Name == b.Name && a.Namespace == b.Namespace ? 1 : -1;
            }
        }

        private static int MethodSignatureMatchWeight(MethodDefinition a, MethodDefinition b, UnhollowerOptions options)
        {
            if (a.Parameters.Count != b.Parameters.Count || a.IsStatic != b.IsStatic ||
                (a.Attributes & MethodAttributes.MemberAccessMask) !=
                (b.Attributes & MethodAttributes.MemberAccessMask))
                return -1;

            var runningSum = TypeMatchWeight(a.ReturnType, b.ReturnType, options);
            if (runningSum == -1)
                return -1;

            void Accumulate(int i)
            {
                if (i < 0 || runningSum < 0)
                    runningSum = -1;
                else
                    runningSum += i;
            }
            
            for (var i = 0; i < a.Parameters.Count; i++)
                Accumulate(TypeMatchWeight(a.Parameters[i].ParameterType, b.Parameters[i].ParameterType, options));

            return runningSum * (a.Parameters.Count + 1);
        }
    }
}