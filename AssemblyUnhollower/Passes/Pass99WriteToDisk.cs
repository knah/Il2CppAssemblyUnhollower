using System.Linq;
using System.Threading.Tasks;
using AssemblyUnhollower.Contexts;

namespace AssemblyUnhollower.Passes
{
    public static class Pass99WriteToDisk
    {
        public static void DoPass(RewriteGlobalContext context, string targetDir)
        {
            var tasks = context.Assemblies.Select(assemblyContext => Task.Run(() => {
                assemblyContext.NewAssembly.Write(targetDir + "/" + assemblyContext.NewAssembly.Name.Name + ".dll");
            })).ToArray();

            Task.WaitAll(tasks);
        }
    }
}