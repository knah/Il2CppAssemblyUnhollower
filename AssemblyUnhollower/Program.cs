using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Passes;
using Iced.Intel;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;

namespace AssemblyUnhollower
{
    class Program
    {
        private struct TimingCookie : IDisposable
        {
            private Stopwatch myStopwatch;
            public TimingCookie(string message)
            {
                Console.Write(message + "... ");
                myStopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                Console.WriteLine($"Done in {myStopwatch.Elapsed}");
            }
        }

        public static void AnalyzeDeobfuscationParams(UnhollowerOptions options)
        {
            RewriteGlobalContext rewriteContext;
            using(new TimingCookie("Reading assemblies"))
                rewriteContext = new RewriteGlobalContext(options, Directory.EnumerateFiles(options.SourceDir, "*.dll"));

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
        private const string ParamUnityDir = "--unity=";
        private const string ParamUniqChars = "--deobf-uniq-chars=";
        private const string ParamUniqMax = "--deobf-uniq-max=";
        private const string ParamAnalyze = "--deobf-analyze";
        private const string ParamBlacklistAssembly = "--blacklist-assembly=";
        private const string ParamVerbose = "--verbose";
        private const string ParamHelp = "--help";
        private const string ParamHelpShort = "-h";
        private const string ParamHelpShortSlash = "/?";

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: AssemblyUnhollower [parameters]");
            Console.WriteLine("Possible parameters:");
            Console.WriteLine($"\t{ParamInputDir}<directory path> - Required. Directory with Il2CppDumper's dummy assemblies");
            Console.WriteLine($"\t{ParamOutputDir}<directory path> - Required. Directory to put results into");
            Console.WriteLine($"\t{ParamMscorlibPath}<file path> - Required. mscorlib.dll of target runtime system (typically loader's)");
            Console.WriteLine($"\t{ParamUnityDir}<directory path> - Optional. Directory with original Unity assemblies for unstripping");
            Console.WriteLine($"\t{ParamUniqChars}<number> - Optional. How many characters per unique token to use during deobfuscation");
            Console.WriteLine($"\t{ParamUniqMax}<number> - Optional. How many maximum unique tokens per type are allowed during deobfuscation");
            Console.WriteLine($"\t{ParamAnalyze} - Optional. Analyze deobfuscation performance with different parameter values. Will not generate assemblies.");
            Console.WriteLine($"\t{ParamBlacklistAssembly}<assembly name> - Optional. Don't write specified assembly to output. Can be used multiple times");
            Console.WriteLine($"\t{ParamVerbose} - Optional. Produce more console output");
            Console.WriteLine($"\t{ParamHelp}, {ParamHelpShort}, {ParamHelpShortSlash} - Optional. Show this help");
            
        }

        public static void Main(string[] args)
        {
            LogSupport.InstallConsoleHandlers();
            
            var options = new UnhollowerOptions();
            options.AdditionalAssembliesBlacklist.Add("Mono.Security"); // always blacklist this one
            var analyze = false;
            
            foreach (var s in args)
            {
                if (s == ParamAnalyze) 
                    analyze = true;
                else if (s == ParamHelp || s == ParamHelpShort || s == ParamHelpShortSlash)
                {
                    PrintUsage();
                    return;
                } else if (s == ParamVerbose)
                    LogSupport.TraceHandler += Console.WriteLine;
                else if (s.StartsWith(ParamInputDir))
                    options.SourceDir = s.Substring(ParamInputDir.Length);
                else if (s.StartsWith(ParamOutputDir))
                    options.OutputDir = s.Substring(ParamOutputDir.Length);
                else if (s.StartsWith(ParamMscorlibPath))
                    options.MscorlibPath = s.Substring(ParamMscorlibPath.Length);
                else if (s.StartsWith(ParamUnityDir))
                    options.UnityBaseLibsDir = s.Substring(ParamUnityDir.Length);
                else if(s.StartsWith(ParamUniqChars))
                    options.TypeDeobfuscationCharsPerUniquifier = Int32.Parse(s.Substring(ParamUniqChars.Length));
                else if(s.StartsWith(ParamUniqMax))
                    options.TypeDeobfuscationMaxUniquifiers = Int32.Parse(s.Substring(ParamUniqMax.Length));
                else if(s.StartsWith(ParamBlacklistAssembly))
                    options.AdditionalAssembliesBlacklist.Add(s.Substring(ParamBlacklistAssembly.Length));
                else
                {
                    Console.WriteLine($"Unrecognized option {s}; use -h for help");
                    return;
                }
            }
            
            if (analyze)
                AnalyzeDeobfuscationParams(options);
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
            if (string.IsNullOrEmpty(options.MscorlibPath))
            {
                Console.WriteLine("No mscorlib specified; use -h for help");
                return;
            }

            if (!Directory.Exists(options.OutputDir))
                Directory.CreateDirectory(options.OutputDir);

            RewriteGlobalContext rewriteContext;
            using(new TimingCookie("Reading assemblies"))
                rewriteContext = new RewriteGlobalContext(options, Directory.EnumerateFiles(options.SourceDir, "*.dll"));

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
            
            using(new TimingCookie("Creating static constructors"))
                Pass20GenerateStaticConstructors.DoPass(rewriteContext);
            using(new TimingCookie("Creating value type fields"))
                Pass21GenerateValueTypeFields.DoPass(rewriteContext);
            using(new TimingCookie("Creating enums"))
                Pass22GenerateEnums.DoPass(rewriteContext);
            using(new TimingCookie("Creating IntPtr constructors"))
                Pass23GeneratePointerConstructors.DoPass(rewriteContext);
            using(new TimingCookie("Creating type getters"))
                Pass24GenerateTypeStaticGetters.DoPass(rewriteContext);
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
                using (new TimingCookie("Unstripping methods"))
                    Pass80UnstripMethods.DoPass(rewriteContext);
            }
            else
                Console.WriteLine("Not performing unstripping as unity libs are not specified");
            
            using(new TimingCookie("Writing assemblies"))
                Pass99WriteToDisk.DoPass(rewriteContext, options);

            File.Copy(typeof(IL2CPP).Assembly.Location, Path.Combine(options.OutputDir, typeof(IL2CPP).Assembly.GetName().Name + ".dll"), true);
            File.Copy(typeof(DelegateSupport).Assembly.Location, Path.Combine(options.OutputDir, typeof(DelegateSupport).Assembly.GetName().Name + ".dll"), true);
            File.Copy(typeof(Decoder).Assembly.Location, Path.Combine(options.OutputDir, typeof(Decoder).Assembly.GetName().Name + ".dll"), true);
            
            Console.WriteLine("Done!");
        }
    }
}