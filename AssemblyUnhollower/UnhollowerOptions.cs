namespace AssemblyUnhollower
{
    public class UnhollowerOptions
    {
        public string SourceDir { get; set; }
        public string OutputDir { get; set; }
        public string MscorlibPath { get; set; }
        public string? UnityBaseLibsDir { get; set; }
        public string[]? AdditionalAssembliesBlacklist { get; set; }
        public int TypeDeobfuscationCharsPerUniquifier { get; set; } = 2;
        public int TypeDeobfuscationMaxUniquifiers { get; set; } = 10;
    }
}