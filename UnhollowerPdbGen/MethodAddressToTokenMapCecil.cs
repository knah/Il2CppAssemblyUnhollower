using System.IO;
using Mono.Cecil;
using UnhollowerBaseLib.Maps;

#nullable enable

namespace UnhollowerPdbGen
{
    public class MethodAddressToTokenMapCecil : MethodAddressToTokenMapBase<AssemblyDefinition, MethodDefinition>
    {
        public MethodAddressToTokenMapCecil(string filePath) : base(filePath)
        {
        }

        protected override AssemblyDefinition? LoadAssembly(string assemblyName)
        {
            var filesDirt = Path.GetDirectoryName(myFilePath)!;
            assemblyName = assemblyName.Substring(0, assemblyName.IndexOf(','));
            return AssemblyDefinition.ReadAssembly(Path.Combine(filesDirt, assemblyName + ".dll"));
        }

        protected override MethodDefinition? ResolveMethod(AssemblyDefinition? assembly, int token)
        {
            return (MethodDefinition?) assembly?.MainModule.LookupToken(token);
        }
    }
}