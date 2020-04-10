using AssemblyUnhollower.Contexts;

namespace AssemblyUnhollower.Passes
{
    public static class Pass99WriteToDisk
    {
        public static void DoPass(RewriteGlobalContext context, string targetDir)
        {
            foreach (var assemblyContext in context.Assemblies)
                assemblyContext.NewAssembly.Write(targetDir + "/" + assemblyContext.NewAssembly.Name.Name + ".dll");
        }
    }
}