using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Passes;
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

        public static void Main(string[] args)
        {
            if (args.Length != 3 && args.Length != 4)
            {
                Console.WriteLine("Usage: AssemblyUnhollower.exe SourceAssemblyDir TargetAssemblyDir mscorlib");
                return;
            }
            
            // todo: better parsing, better usage

            var unhollowerOptions = new UnhollowerOptions
            {
                SourceDir = args[0],
                OutputDir = args[1],
                MscorlibPath = args[2],
            };
            if (args.Length == 4)
                AnalyzeDeobfuscationParams(unhollowerOptions);
            else
                Main(unhollowerOptions);
        }
        
        public static void Main(UnhollowerOptions options)
        {
            if (options.UnityBaseLibsDir != null)
                Console.WriteLine(
                    "Unity libs path is specified; this will currently do nothing, as unity unstripping is not yet implemented");

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
            
            using(new TimingCookie("Writing assemblies"))
                Pass99WriteToDisk.DoPass(rewriteContext, options.OutputDir);

            File.Copy(typeof(IL2CPP).Assembly.Location, Path.Combine(options.OutputDir, typeof(IL2CPP).Assembly.GetName().Name + ".dll"), true);
            File.Copy(typeof(DelegateSupport).Assembly.Location, Path.Combine(options.OutputDir, typeof(DelegateSupport).Assembly.GetName().Name + ".dll"), true);
            
            Console.WriteLine("Done!");
        }
    }
}