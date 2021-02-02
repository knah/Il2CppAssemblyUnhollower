using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AssemblyUnhollower
{
    public class UnhollowerOptions
    {
        public string SourceDir { get; set; }
        public string OutputDir { get; set; }
        public string MscorlibPath { get; set; }
        public string? UnityBaseLibsDir { get; set; }
        public List<string> AdditionalAssembliesBlacklist { get; } = new List<string>();
        public int TypeDeobfuscationCharsPerUniquifier { get; set; } = 2;
        public int TypeDeobfuscationMaxUniquifiers { get; set; } = 10;
        public string GameAssemblyPath { get; set; }
        public bool Verbose { get; set; }
        public bool NoXrefCache { get; set; }
        public bool NoCopyUnhollowerLibs { get; set; }
        public Regex? ObfuscatedNamesRegex { get; set; }
        public Dictionary<string, string> RenameMap { get; } = new Dictionary<string, string>();
        public bool PassthroughNames { get; set; }
        
        public List<string> DeobfuscationGenerationAssemblies { get; } = new List<string>();
        public string DeobfuscationNewAssembliesPath { get; set; }
        
    }
}