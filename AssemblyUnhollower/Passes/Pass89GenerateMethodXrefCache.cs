using System.Collections.Generic;
using System.IO;
using System.Text;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;
using UnhollowerBaseLib.Attributes;
using UnhollowerBaseLib.Maps;
using UnhollowerRuntimeLib.XrefScans;

namespace AssemblyUnhollower.Passes
{
    public static class Pass89GenerateMethodXrefCache
    {
        public static void DoPass(RewriteGlobalContext context, UnhollowerOptions options)
        {
            var data = new List<MethodXrefScanCache.MethodData>();
            var existingAttributesPerAddress = new Dictionary<long, CachedScanResultsAttribute>();

            if (options.NoXrefCache)
                goto skipDataGather;
            
            foreach (var assemblyRewriteContext in context.Assemblies)
            {
                if (options.AdditionalAssembliesBlacklist.Contains(assemblyRewriteContext.NewAssembly.Name.Name))
                    continue;
                
                var imports = assemblyRewriteContext.Imports;

                foreach (var typeRewriteContext in assemblyRewriteContext.Types)
                {
                    foreach (var methodRewriteContext in typeRewriteContext.Methods)
                    {
                        var address = methodRewriteContext.Rva;

                        if (existingAttributesPerAddress.TryGetValue(address, out var attribute))
                        {
                            methodRewriteContext.NewMethod.CustomAttributes.Add(
                                new CustomAttribute(imports.CachedScanResultsAttributeCtor)
                                {
                                    Fields =
                                    {
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.RefRangeStart),
                                            new CustomAttributeArgument(imports.Int, attribute.RefRangeStart)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.RefRangeEnd),
                                            new CustomAttributeArgument(imports.Int, attribute.RefRangeEnd)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.XrefRangeStart),
                                            new CustomAttributeArgument(imports.Int, attribute.RefRangeStart)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.XrefRangeEnd),
                                            new CustomAttributeArgument(imports.Int, attribute.RefRangeEnd)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.MetadataInitTokenRva),
                                            new CustomAttributeArgument(imports.Int, attribute.MetadataInitTokenRva)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.MetadataInitFlagRva),
                                            new CustomAttributeArgument(imports.Int, attribute.MetadataInitFlagRva)),
                                    }
                                });
                            continue;
                        }

                        var xrefStart = data.Count;
                        
                        foreach (var xrefScanResult in methodRewriteContext.XrefScanResults)
                            data.Add(MethodXrefScanCache.MethodData.FromXrefInstance(xrefScanResult));

                        var xrefEnd = data.Count;

                        var refStart = 0;
                        var refEnd = 0;
                        
                        if (address != 0)
                        {
                            if (Pass16ScanMethodRefs.MapOfCallers.TryGetValue(address, out var callerMap))
                            {
                                refStart = data.Count;
                                foreach (var xrefInstance in callerMap)
                                    data.Add(MethodXrefScanCache.MethodData.FromXrefInstance(xrefInstance));

                                refEnd = data.Count;
                            }
                        }

                        if (xrefEnd != xrefStart || refStart != refEnd)
                        {
                            methodRewriteContext.NewMethod.CustomAttributes.Add(
                                new CustomAttribute(imports.CachedScanResultsAttributeCtor)
                                {
                                    Fields =
                                    {
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.RefRangeStart),
                                            new CustomAttributeArgument(imports.Int, refStart)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.RefRangeEnd),
                                            new CustomAttributeArgument(imports.Int, refEnd)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.XrefRangeStart),
                                            new CustomAttributeArgument(imports.Int, xrefStart)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.XrefRangeEnd),
                                            new CustomAttributeArgument(imports.Int, xrefEnd)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.MetadataInitTokenRva),
                                            new CustomAttributeArgument(imports.Long, methodRewriteContext.MetadataInitTokenRva)),
                                        new CustomAttributeNamedArgument(
                                            nameof(CachedScanResultsAttribute.MetadataInitFlagRva),
                                            new CustomAttributeArgument(imports.Long, methodRewriteContext.MetadataInitFlagRva)),
                                    }
                                });

                            existingAttributesPerAddress[address] = new CachedScanResultsAttribute
                            {
                                RefRangeStart = refStart,
                                RefRangeEnd = refEnd,
                                XrefRangeStart = xrefStart,
                                XrefRangeEnd = xrefEnd,
                                MetadataInitFlagRva = methodRewriteContext.MetadataInitFlagRva,
                                MetadataInitTokenRva = methodRewriteContext.MetadataInitTokenRva
                            };
                        }
                    }
                }
            }
            
            skipDataGather:
            
            var header = new MethodXrefScanCache.FileHeader
            {
                Magic = MethodXrefScanCache.Magic,
                Version = MethodXrefScanCache.Version,
                InitMethodMetadataRva = XrefScanMetadataGenerationUtil.MetadataInitForMethodRva
            };
            
            using var writer = new BinaryWriter(new FileStream(Path.Combine(options.OutputDir, MethodXrefScanCache.FileName), FileMode.Create, FileAccess.Write), Encoding.UTF8, false);
            writer.Write(header);

            foreach (var valueTuple in data) 
                writer.Write(valueTuple);

            if (options.Verbose)
            {
                using var plainTextWriter = new StreamWriter(Path.Combine(options.OutputDir, MethodXrefScanCache.FileName + ".txt"));
                for (var i = 0; i < data.Count; i++)
                {
                    plainTextWriter.WriteLine($"{i}\t{data[i].Type}\t{data[i].Address}\t{data[i].FoundAt}");
                }
            }
        }
    }
}