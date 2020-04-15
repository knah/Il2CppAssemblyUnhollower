using System;
using System.Diagnostics;
using System.IO;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Passes;
using UnhollowerBaseLib;

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
        
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: AssemblyUnhollower.exe SourceAssemblyDir TargetAssemblyDir mscorlib");
                return;
            }

            var sourceDir = args[0];
            var targetDir = args[1];
            
            Console.WriteLine("Reading assemblies");
            var rewriteContext = new RewriteGlobalContext(args[2], Directory.EnumerateFiles(sourceDir, "*.dll"));
            
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
            Pass60AddImplicitConversions.DoPass(rewriteContext);
            using(new TimingCookie("Creating properties"))
                Pass70GenerateProperties.DoPass(rewriteContext);
            
            using(new TimingCookie("Writing assemblies"))
                Pass99WriteToDisk.DoPass(rewriteContext, targetDir);

            File.Copy(typeof(IL2CPP).Assembly.Location, Path.Combine(targetDir, typeof(IL2CPP).Assembly.GetName().Name + ".dll"), true);
            
            Console.WriteLine("Done!");
        }
    }
}