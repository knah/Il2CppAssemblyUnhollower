using System;
using System.IO;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Passes;

namespace AssemblyUnhollower
{
    class Program
    {
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
            
            Console.WriteLine("Creating typedefs");
            Pass10CreateTypedefs.DoPass(rewriteContext);
            Console.WriteLine("Filling typedefs");
            Pass11FillTypedefs.DoPass(rewriteContext);
            Console.WriteLine("Filling generic constraints");
            Pass12FillGenericConstraints.DoPass(rewriteContext);
            Console.WriteLine("Creating members");
            Pass15GenerateMemberContexts.DoPass(rewriteContext);
            
            Console.WriteLine("Creating static constructors");
            Pass20GenerateStaticConstructors.DoPass(rewriteContext);
            Console.WriteLine("Creating value type fields");
            Pass21GenerateValueTypeFields.DoPass(rewriteContext);
            Console.WriteLine("Creating enums");
            Pass22GenerateEnums.DoPass(rewriteContext);
            Console.WriteLine("Creating IntPtr constructors");
            Pass23GeneratePointerConstructors.DoPass(rewriteContext);
            Console.WriteLine("Creating type getters");
            Pass24GenerateTypeStaticGetters.DoPass(rewriteContext);
            
            Console.WriteLine("Creating generic method static constructors");
            Pass30GenerateGenericMethodStoreConstructors.DoPass(rewriteContext);
            Console.WriteLine("Creating field accessors");
            Pass40GenerateFieldAccessors.DoPass(rewriteContext);
            Console.WriteLine("Filling methods");
            Pass50GenerateMethods.DoPass(rewriteContext);
            Console.WriteLine("Creating properties");
            Pass70GenerateProperties.DoPass(rewriteContext);
            
            Console.WriteLine("Writing assemblies");
            Pass99WriteToDisk.DoPass(rewriteContext, targetDir);
        }
    }
}