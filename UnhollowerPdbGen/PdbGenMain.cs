using System;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace UnhollowerPdbGen
{
    public class PdbGenMain
    {
        public static void Main(string[] args)
        {
            if (args.Length <= 1)
            {
                Console.WriteLine($"Usage: UnhollowerPdbGen.exe <path to GameAssembly.dll> <path to {MethodAddressToTokenMapCecil.FileName}>");
            }
            var rootPath = Path.GetDirectoryName(args[0])!;
            var map = new MethodAddressToTokenMapCecil(args[1]);

            using var peStream = new FileStream(args[0], FileMode.Open, FileAccess.Read);
            using var peReader = new PEReader(peStream);


            string openError;
            PDBErrors err;
            var pdbFilePath = Path.Combine(rootPath, "GameAssembly.pdb");
            MsPdbCore.PDBOpen2W(pdbFilePath, "w", out err, out openError, out var pdb);

            MsPdbCore.PDBOpenDBI(pdb, "w", "", out var dbi);

            MsPdbCore.DBIOpenModW(dbi, "__Globals", "__Globals", out var mod);

            ushort secNum = 1;
            ushort i2cs = 1;
            foreach (var sectionHeader in peReader.PEHeaders.SectionHeaders)
            {
                if (sectionHeader.Name == "il2cpp") i2cs = secNum;
                MsPdbCore.DBIAddSec(dbi, secNum++, 0 /* TODO? */, sectionHeader.VirtualAddress, sectionHeader.VirtualSize);
            }
            
            foreach (var valueTuple in map)
            {
                ushort targetSect = 0;
                long tsva = 0;
                ushort sc = 1;
                foreach (var sectionHeader in peReader.PEHeaders.SectionHeaders)
                {
                    if (valueTuple.Item1 > sectionHeader.VirtualAddress)
                    {
                        targetSect = sc;
                        tsva = sectionHeader.VirtualAddress;
                    }
                    else
                        break;

                    sc++;
                }

                if (targetSect == 0) throw new ApplicationException("Bad segment");
                MsPdbCore.ModAddPublic2(mod, valueTuple.Item2.FullName, targetSect, (int)(valueTuple.Item1 - tsva * 2), CV_PUBSYMFLAGS_e.cvpsfFunction);
            }
            
            MsPdbCore.ModClose(mod);
            MsPdbCore.DBIClose(dbi);

            MsPdbCore.PDBCommit(pdb);
            
            MsPdbCore.PDBQuerySignature2(pdb, out var wrongGuid);
            
            MsPdbCore.PDBClose(pdb);
            
            // Hack: manually replace guid and age in generated .pdb, because there's no API on mspdbcore to set them manually
            var targetDebugInfo = peReader.ReadCodeViewDebugDirectoryData(peReader.ReadDebugDirectory()
                .Single(it => it.Type == DebugDirectoryEntryType.CodeView));

            var wrongGuidBytes = wrongGuid.ToByteArray();
            var allPdbBytes = File.ReadAllBytes(pdbFilePath);

            var patchTarget = IndexOfBytes(allPdbBytes, wrongGuidBytes);
            targetDebugInfo.Guid.TryWriteBytes(allPdbBytes.AsSpan(patchTarget));
            
            Console.WriteLine(targetDebugInfo.Guid);
            Console.WriteLine(targetDebugInfo.Age);

            BitConverter.TryWriteBytes(allPdbBytes.AsSpan(patchTarget - 4), targetDebugInfo.Age);
            File.WriteAllBytes(pdbFilePath, allPdbBytes);
        }

        private static int IndexOfBytes(byte[] haystack, byte[] needle)
        {
            for (var i = 0; i < haystack.Length - needle.Length; i++)
            {
                for (var j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                        goto moveOn;
                }

                return i;
                moveOn: ;
            }

            return -1;
        }
    }
}