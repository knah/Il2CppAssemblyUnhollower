using AssemblyUnhollower.Contexts;

namespace AssemblyUnhollower.Passes
{
    public static class Pass15GenerateMemberContexts
    {
        public static bool HasObfuscatedMethods;
        
        public static void DoPass(RewriteGlobalContext context)
        {
            foreach (var assemblyContext in context.Assemblies)
            foreach (var typeContext in assemblyContext.Types)
                typeContext.AddMembers();
        }
    }
}