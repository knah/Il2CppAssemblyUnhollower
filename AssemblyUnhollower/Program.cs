using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.MetadataAccess;
using AssemblyUnhollower.Passes;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using Decoder = Iced.Intel.Decoder;

namespace AssemblyUnhollower
{
    public class Program
    {
        public static void AnalyzeDeobfuscationParams(UnhollowerOptions options)
        {
            RewriteGlobalContext rewriteContext;
            IIl2CppMetadataAccess inputAssemblies;
            using (new TimingCookie("Reading assemblies"))
                inputAssemblies = new CecilMetadataAccess(Directory.EnumerateFiles(options.SourceDir, "*.dll"));
            
            using(new TimingCookie("Creating assembly contexts"))
                rewriteContext = new RewriteGlobalContext(options, inputAssemblies, NullMetadataAccess.Instance, NullMetadataAccess.Instance);

            for (var chars = 1; chars <= 3; chars++)
            for (var uniq = 3; uniq <= 15; uniq++)
            {
                options.TypeDeobfuscationCharsPerUniquifier = chars;
                options.TypeDeobfuscationMaxUniquifiers = uniq;
                
                rewriteContext.RenamedTypes.Clear();
                rewriteContext.RenameGroups.Clear();

                Pass05CreateRenameGroups.DoPass(rewriteContext);

                var uniqueTypes = rewriteContext.RenameGroups.Values.Count(it => it.Count == 1);
                var nonUniqueTypes = rewriteContext.RenameGroups.Values.Count(it => it.Count > 1);
                
                Console.WriteLine($"Chars=\t{chars}\tMaxU=\t{uniq}\tUniq=\t{uniqueTypes}\tNonUniq=\t{nonUniqueTypes}");
            }
        }

        private const string ParamInputDir = "--input=";
        private const string ParamOutputDir = "--output=";
        private const string ParamMscorlibPath = "--mscorlib=";
        private const string ParamSystemLibsPath = "--system-libs=";
        private const string ParamUnityDir = "--unity=";
        private const string ParamGameAssemblyPath = "--gameassembly=";
        private const string ParamUniqChars = "--deobf-uniq-chars=";
        private const string ParamUniqMax = "--deobf-uniq-max=";
        private const string ParamAnalyze = "--deobf-analyze";
        private const string ParamGenerateDeobMap = "--deobf-generate";
        private const string ParamGenerateDeobMapAssembly = "--deobf-generate-asm=";
        private const string ParamGenerateDeobMapNew = "--deobf-generate-new=";
        private const string ParamBlacklistAssembly = "--blacklist-assembly=";
        private const string ParamNoXrefCache = "--no-xref-cache";
        private const string ParamNoCopyUnhollowerLibs = "--no-copy-unhollower-libs";
        private const string ParamObfRegex = "--obf-regex=";
        private const string ParamRenameMap = "--rename-map=";
        private const string ParamPassthroughNames = "--passthrough-names";
        private const string ParamVerbose = "--verbose";
        private const string ParamHelp = "--help";
        private const string ParamHelpShort = "-h";
        private const string ParamHelpShortSlash = "/?";

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: AssemblyUnhollower [parameters]");
            Console.WriteLine("Possible parameters:");
            Console.WriteLine($"\t{ParamHelp}, {ParamHelpShort}, {ParamHelpShortSlash} - Optional. Show this help");
            Console.WriteLine($"\t{ParamVerbose} - Optional. Produce more console output");
            Console.WriteLine($"\t{ParamInputDir}<directory path> - Required. Directory with Il2CppDumper's dummy assemblies");
            Console.WriteLine($"\t{ParamOutputDir}<directory path> - Required. Directory to put results into");
            Console.WriteLine($"\t{ParamMscorlibPath}<file path> - Deprecated. mscorlib.dll of target runtime system (typically loader's)");
            Console.WriteLine($"\t{ParamSystemLibsPath}<file path> - Required. Directory with system libraries of target runtime system (typically loader's)");
            Console.WriteLine($"\t{ParamUnityDir}<directory path> - Optional. Directory with original Unity assemblies for unstripping");
            Console.WriteLine($"\t{ParamGameAssemblyPath}<file path> - Optional. Path to GameAssembly.dll. Used for certain analyses");
            Console.WriteLine($"\t{ParamUniqChars}<number> - Optional. How many characters per unique token to use during deobfuscation");
            Console.WriteLine($"\t{ParamUniqMax}<number> - Optional. How many maximum unique tokens per type are allowed during deobfuscation");
            Console.WriteLine($"\t{ParamAnalyze} - Optional. Analyze deobfuscation performance with different parameter values. Will not generate assemblies.");
            Console.WriteLine($"\t{ParamBlacklistAssembly}<assembly name> - Optional. Don't write specified assembly to output. Can be used multiple times");
            Console.WriteLine($"\t{ParamNoXrefCache} - Optional. Don't generate xref scanning cache. All scanning will be done at runtime.");
            Console.WriteLine($"\t{ParamNoCopyUnhollowerLibs} - Optional. Don't copy unhollower libraries to output directory");
            Console.WriteLine($"\t{ParamObfRegex}<regex> - Optional. Specifies a regex for obfuscated names. All types and members matching will be renamed");
            Console.WriteLine($"\t{ParamRenameMap}<file path> - Optional. Specifies a file specifying rename map for obfuscated types and members");
            Console.WriteLine($"\t{ParamPassthroughNames} - Optional. If specified, names will be copied from input assemblies as-is without renaming or deobfuscation");
            Console.WriteLine("Deobfuscation map generation mode:");
            Console.WriteLine($"\t{ParamGenerateDeobMap} - Generate a deobfuscation map for input files. Will not generate assemblies.");
            Console.WriteLine($"\t{ParamGenerateDeobMapAssembly}<assembly name> - Optional. Include this assembly for deobfuscation map generation. If none are specified, all assemblies will be included.");
            Console.WriteLine($"\t{ParamGenerateDeobMapNew}<directory path> - Required. Specifies the directory with new (obfuscated) assemblies. The --input parameter specifies old (unobfuscated) assemblies.");
        }

        public static void Main(string[] args)
        {
            LogSupport.InstallConsoleHandlers();
            
            var options = new UnhollowerOptions();
            var analyze = false;
            var generateMap = false;
            
            foreach (var s in args)
            {
                if (s == ParamAnalyze) 
                    analyze = true;
                else if (s == ParamGenerateDeobMap)
                    generateMap = true;
                else if (s == ParamHelp || s == ParamHelpShort || s == ParamHelpShortSlash)
                {
                    PrintUsage();
                    return;
                } else if (s == ParamVerbose)
                {
                    LogSupport.TraceHandler += Console.WriteLine;
                    options.Verbose = true;
                } else if (s == ParamNoXrefCache)
                    options.NoXrefCache = true;
                else if (s == ParamNoCopyUnhollowerLibs)
                    options.NoCopyUnhollowerLibs = true;
                else if (s.StartsWith(ParamInputDir))
                    options.SourceDir = s.Substring(ParamInputDir.Length);
                else if (s.StartsWith(ParamOutputDir))
                    options.OutputDir = s.Substring(ParamOutputDir.Length);
                else if (s.StartsWith(ParamMscorlibPath))
                    options.MscorlibPath = s.Substring(ParamMscorlibPath.Length);
                else if (s.StartsWith(ParamSystemLibsPath))
                    options.SystemLibrariesPath = s.Substring(ParamSystemLibsPath.Length);
                else if (s.StartsWith(ParamUnityDir))
                    options.UnityBaseLibsDir = s.Substring(ParamUnityDir.Length);
                else if (s.StartsWith(ParamGameAssemblyPath))
                    options.GameAssemblyPath = s.Substring(ParamGameAssemblyPath.Length);
                else if(s.StartsWith(ParamUniqChars))
                    options.TypeDeobfuscationCharsPerUniquifier = Int32.Parse(s.Substring(ParamUniqChars.Length));
                else if(s.StartsWith(ParamUniqMax))
                    options.TypeDeobfuscationMaxUniquifiers = Int32.Parse(s.Substring(ParamUniqMax.Length));
                else if(s.StartsWith(ParamBlacklistAssembly))
                    options.AdditionalAssembliesBlacklist.Add(s.Substring(ParamBlacklistAssembly.Length));
                else if (s.StartsWith(ParamObfRegex))
                    options.ObfuscatedNamesRegex = new Regex(s.Substring(ParamObfRegex.Length), RegexOptions.Compiled);
                else if(s.StartsWith(ParamRenameMap))
                    ReadRenameMap(s.Substring(ParamRenameMap.Length), options);
                else if(s.StartsWith(ParamGenerateDeobMapAssembly))
                    options.DeobfuscationGenerationAssemblies.Add(s.Substring(ParamGenerateDeobMapAssembly.Length));
                else if (s.StartsWith(ParamGenerateDeobMapNew))
                    options.DeobfuscationNewAssembliesPath = s.Substring(ParamGenerateDeobMapNew.Length);
                else
                {
                    LogSupport.Error($"Unrecognized option {s}; use -h for help");
                    return;
                }
            }

            if (analyze && generateMap)
            {
                LogSupport.Error($"Can't use {ParamAnalyze} and {ParamGenerateDeobMap} at the same time");
                return;
            }
            
            if (analyze)
                AnalyzeDeobfuscationParams(options);
            else if (generateMap)
                DeobfuscationMapGenerator.GenerateDeobfuscationMap(options);
            else
                Main(options);
        }
        
        public static void Main(UnhollowerOptions options)
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
            if (string.IsNullOrEmpty(options.MscorlibPath) && string.IsNullOrEmpty(options.SystemLibrariesPath))
            {
                Console.WriteLine("No mscorlib or system libraries specified; use -h for help");
                return;
            }

            if (!Directory.Exists(options.OutputDir))
                Directory.CreateDirectory(options.OutputDir);

            RewriteGlobalContext rewriteContext;
            IIl2CppMetadataAccess gameAssemblies;
            IMetadataAccess systemAssemblies;
            IMetadataAccess unityAssemblies;

            using (new TimingCookie("Reading assemblies"))
                gameAssemblies = new CecilMetadataAccess(Directory.EnumerateFiles(options.SourceDir, "*.dll"));

            using (new TimingCookie("Reading system assemblies"))
            {
                if (!string.IsNullOrEmpty(options.SystemLibrariesPath)) 
                    systemAssemblies = new CecilMetadataAccess(Directory.EnumerateFiles(options.SystemLibrariesPath, "*.dll")
                        .Where(it => Path.GetFileName(it).StartsWith("System.") || Path.GetFileName(it) == "mscorlib.dll" || Path.GetFileName(it) == "netstandard.dll"));
                else
                    systemAssemblies = new CecilMetadataAccess(new[] {options.MscorlibPath});

            }

            if (!string.IsNullOrEmpty(options.UnityBaseLibsDir))
            {
                using (new TimingCookie("Reading unity assemblies"))
                    unityAssemblies = new CecilMetadataAccess(Directory.EnumerateFiles(options.UnityBaseLibsDir, "*.dll"));
            }
            else
                unityAssemblies = NullMetadataAccess.Instance;

            using(new TimingCookie("Creating rewrite assemblies"))
                rewriteContext = new RewriteGlobalContext(options, gameAssemblies, systemAssemblies, unityAssemblies);

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
            using(new TimingCookie("Scanning method cross-references"))
                Pass16ScanMethodRefs.DoPass(rewriteContext, options);
            using(new TimingCookie("Finalizing method declarations"))
                Pass18FinalizeMethodContexts.DoPass(rewriteContext);
            LogSupport.Info($"{Pass18FinalizeMethodContexts.TotalPotentiallyDeadMethods} total potentially dead methods");
            using(new TimingCookie("Filling method parameters"))
                Pass19CopyMethodParameters.DoPass(rewriteContext);
            
            using(new TimingCookie("Creating static constructors"))
                Pass20GenerateStaticConstructors.DoPass(rewriteContext);
            using(new TimingCookie("Creating value type fields"))
                Pass21GenerateValueTypeFields.DoPass(rewriteContext);
            using(new TimingCookie("Creating enums"))
                Pass22GenerateEnums.DoPass(rewriteContext);
            using(new TimingCookie("Creating IntPtr constructors"))
                Pass23GeneratePointerConstructors.DoPass(rewriteContext);
            using(new TimingCookie("Creating non-blittable struct constructors"))
                Pass25GenerateNonBlittableValueTypeDefaultCtors.DoPass(rewriteContext);
            
            using(new TimingCookie("Creating generic method static constructors"))
                Pass30GenerateGenericMethodStoreConstructors.DoPass(rewriteContext);
            using(new TimingCookie("Creating field accessors"))
                Pass40GenerateFieldAccessors.DoPass(rewriteContext);
            using(new TimingCookie("Filling methods"))
                Pass50GenerateMethods.DoPass(rewriteContext);
            using(new TimingCookie("Generating implicit conversions"))
                Pass60AddImplicitConversions.DoPass(rewriteContext);
            using(new TimingCookie("Creating properties"))
                Pass70GenerateProperties.DoPass(rewriteContext);

            if (options.UnityBaseLibsDir != null)
            {
                using (new TimingCookie("Unstripping types"))
                    Pass79UnstripTypes.DoPass(rewriteContext);
                using (new TimingCookie("Unstripping fields"))
                    Pass80UnstripFields.DoPass(rewriteContext);
                using (new TimingCookie("Unstripping methods"))
                    Pass80UnstripMethods.DoPass(rewriteContext);
                using (new TimingCookie("Unstripping method bodies"))
                    Pass81FillUnstrippedMethodBodies.DoPass(rewriteContext);
            }
            else
                LogSupport.Warning("Not performing unstripping as unity libs are not specified");
            
            using(new TimingCookie("Generating forwarded types"))
                Pass89GenerateForwarders.DoPass(rewriteContext);
            
            using(new TimingCookie("Writing xref cache"))
                Pass89GenerateMethodXrefCache.DoPass(rewriteContext, options);
            
            using(new TimingCookie("Writing assemblies"))
                Pass90WriteToDisk.DoPass(rewriteContext, options);
            
            using(new TimingCookie("Writing method pointer map"))
                Pass91GenerateMethodPointerMap.DoPass(rewriteContext, options);
            
            using(new TimingCookie("Writing type token map"))
                Pass91GenerateTypeTokenMap.DoPass(rewriteContext, options);

            if (!options.NoCopyUnhollowerLibs)
            {
                File.Copy(typeof(IL2CPP).Assembly.Location, Path.Combine(options.OutputDir, typeof(IL2CPP).Assembly.GetName().Name + ".dll"), true);
                File.Copy(typeof(RuntimeLibMarker).Assembly.Location, Path.Combine(options.OutputDir, typeof(RuntimeLibMarker).Assembly.GetName().Name + ".dll"), true);
                File.Copy(typeof(Decoder).Assembly.Location, Path.Combine(options.OutputDir, typeof(Decoder).Assembly.GetName().Name + ".dll"), true);
            }
            
            LogSupport.Info("Done!");

            rewriteContext.Dispose();
        }

        /// <summary>
        /// Reads a rename map from the specified name into the specified instance of options
        /// </summary>
        public static void ReadRenameMap(string fileName, UnhollowerOptions options)
        {
            using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            ReadRenameMap(fileStream, fileName.EndsWith(".gz"), options);
        }

        /// <summary>
        /// Reads a rename map from the specified name into the specified instance of options.
        /// The stream is not closed by this method.
        /// </summary>
        public static void ReadRenameMap(Stream fileStream, bool isGzip, UnhollowerOptions options)
        {
            if (isGzip)
            {
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress, true);
                ReadRenameMap(gzipStream, false, options);
                return;
            }

            using var reader = new StreamReader(fileStream, Encoding.UTF8, false, 65536, true);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if(string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
                var split = line.Split(';');
                if(split.Length < 2) continue;
                options.RenameMap[split[0]] = split[1];
            }
        }
    }
}