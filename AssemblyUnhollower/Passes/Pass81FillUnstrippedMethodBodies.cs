using System.Collections.Generic;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Utils;
using Mono.Cecil;
using UnhollowerBaseLib;

namespace AssemblyUnhollower.Passes
{
    public static class Pass81FillUnstrippedMethodBodies
    {
        private static readonly
            List<(MethodDefinition unityMethod, MethodDefinition newMethod, TypeRewriteContext processedType, AssemblyKnownImports imports)> StuffToProcess =
                new List<(MethodDefinition unityMethod, MethodDefinition newMethod, TypeRewriteContext processedType, AssemblyKnownImports imports)>();
        
        public static void DoPass(RewriteGlobalContext context)
        {
            int methodsSucceeded = 0;
            int methodsFailed = 0;
            
            foreach (var (unityMethod, newMethod, processedType, imports) in StuffToProcess)
            {
                var success = UnstripTranslator.TranslateMethod(unityMethod, newMethod, processedType, imports);
                if (success == false)
                {
                    methodsFailed++;
                    UnstripTranslator.ReplaceBodyWithException(newMethod, imports);
                }
                else
                    methodsSucceeded++;
            }
            
            LogSupport.Info(""); // finish progress line
            LogSupport.Info($"IL unstrip statistics: {methodsSucceeded} successful, {methodsFailed} failed");
        }

        public static void PushMethod(MethodDefinition unityMethod, MethodDefinition newMethod, TypeRewriteContext processedType, AssemblyKnownImports imports)
        {
            StuffToProcess.Add((unityMethod, newMethod, processedType, imports));
        }
    }
}