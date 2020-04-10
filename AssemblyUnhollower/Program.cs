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
            
            var rewriteContext = new RewriteGlobalContext(args[2], Directory.EnumerateFiles(sourceDir, "*.dll"));
            
            Pass10CreateTypedefs.DoPass(rewriteContext);
            Pass11FillTypedefs.DoPass(rewriteContext);
            Pass12FillGenericConstraints.DoPass(rewriteContext);
            Pass15GenerateMemberContexts.DoPass(rewriteContext);
            
            Pass20GenerateStaticConstructors.DoPass(rewriteContext);
            Pass21GenerateValueTypeFields.DoPass(rewriteContext);
            Pass22GenerateEnums.DoPass(rewriteContext);
            Pass23GeneratePointerConstructors.DoPass(rewriteContext);
            Pass24GenerateTypeStaticGetters.DoPass(rewriteContext);
            
            Pass30GenerateGenericMethodStoreConstructors.DoPass(rewriteContext);
            Pass40GenerateFieldAccessors.DoPass(rewriteContext);
            Pass50GenerateMethods.DoPass(rewriteContext);
            Pass70GenerateProperties.DoPass(rewriteContext);
            
            Pass99WriteToDisk.DoPass(rewriteContext, targetDir);
        }
    }
}