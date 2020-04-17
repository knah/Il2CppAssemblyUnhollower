using Mono.Cecil;

namespace AssemblyUnhollower
{
    public static class TargetTypeSystemHandler
    {
        public static TypeReference Void { get; private set; }
        public static TypeReference IntPtr { get; private set; }
        public static TypeDefinition String { get; private set; }
        public static TypeDefinition Type { get; private set; }
        public static TypeReference Object { get; private set; }
        public static TypeReference Enum { get; private set; }
        public static TypeReference ValueType { get; private set; }
        public static TypeReference Delegate { get; private set; }

        public static void Init(AssemblyDefinition mscorlib)
        {
            Void = mscorlib.MainModule.TypeSystem.Void;
            IntPtr = mscorlib.MainModule.TypeSystem.IntPtr;
            String = mscorlib.MainModule.GetType("System.String");
            Type = mscorlib.MainModule.GetType("System.Type");
            Object = mscorlib.MainModule.TypeSystem.Object;
            Enum = mscorlib.MainModule.GetType("System.Enum");
            ValueType = mscorlib.MainModule.GetType("System.ValueType");
            Delegate = mscorlib.MainModule.GetType("System.Delegate");
        }
    }
}