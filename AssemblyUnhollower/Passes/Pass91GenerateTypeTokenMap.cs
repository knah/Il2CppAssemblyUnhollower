using System.Collections.Generic;
using System.IO;
using System.Text;
using AssemblyUnhollower.Contexts;
using AssemblyUnhollower.Extensions;
using Mono.Cecil;
using UnhollowerBaseLib.Maps;

namespace AssemblyUnhollower.Passes
{
    public static class Pass91GenerateTypeTokenMap
    {
        public static void DoPass(RewriteGlobalContext context, UnhollowerOptions options)
        {
            var fileHeader = new TypeTokensMap.FileHeader();

            var rawData = new List<int>();
            var perAssemblyData = new List<(string NativeAssembly, string ManagedAssembly, int TokenRangeStart, int TokenRangeEnd, int TokenValuesOffset)>();

            var assemblyList = new List<(int il2CppToken, int managedToken)>();
            foreach (var assemblyContext in context.Assemblies)
            {
                assemblyList.Clear();

                foreach (var assemblyType in assemblyContext.Types)
                {
                    if (assemblyType.Il2CppToken == 0) continue;
                    
                    assemblyList.Add((assemblyType.Il2CppToken, assemblyType.NewType.MetadataToken.ToInt32()));
                }
                
                assemblyList.Sort((a, b) => a.il2CppToken.CompareTo(b.il2CppToken));

                perAssemblyData.Add((assemblyContext.OriginalAssembly.Name.Name, assemblyContext.NewAssembly.Name.Name,
                    rawData.Count, rawData.Count + assemblyList.Count, rawData.Count + assemblyList.Count));
                
                foreach (var valueTuple in assemblyList) 
                    rawData.Add(valueTuple.il2CppToken);
                
                foreach (var valueTuple in assemblyList) 
                    rawData.Add(valueTuple.managedToken);
            }

            fileHeader.Magic = TypeTokensMap.Magic;
            fileHeader.Version = TypeTokensMap.Version;
            fileHeader.NumAssemblies = perAssemblyData.Count;
            fileHeader.DataOffset = 0;
            
            using var writer = new BinaryWriter(new FileStream(Path.Combine(options.OutputDir, TypeTokensMap.FileName), FileMode.Create, FileAccess.Write), Encoding.UTF8, false);
            writer.Write(fileHeader);
            
            foreach (var valueTuple in perAssemblyData)
            {
                writer.Write(valueTuple.NativeAssembly);
                writer.Write(valueTuple.ManagedAssembly);
                writer.Write(valueTuple.TokenRangeStart);
                writer.Write(valueTuple.TokenRangeEnd);
                writer.Write(valueTuple.TokenValuesOffset);
            }

            fileHeader.DataOffset = (int) writer.BaseStream.Position;
            foreach (var i in rawData) 
                writer.Write(i);

            writer.BaseStream.Position = 0;
            writer.Write(fileHeader);
        }
    }
}