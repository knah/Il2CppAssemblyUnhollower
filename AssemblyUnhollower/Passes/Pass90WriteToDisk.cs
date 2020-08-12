using System.Linq;
using System.Threading.Tasks;
using AssemblyUnhollower.Contexts;

namespace AssemblyUnhollower.Passes
{
    public static class Pass90WriteToDisk
    {
        public static void DoPass(RewriteGlobalContext context, UnhollowerOptions options)
        {
            var tasks = context.Assemblies.Where(it => !options.AdditionalAssembliesBlacklist.Contains(it.NewAssembly.Name.Name)).Select(assemblyContext => Task.Run(() => {
                assemblyContext.NewAssembly.Write(options.OutputDir + "/" + assemblyContext.NewAssembly.Name.Name + ".dll");
            })).ToArray();

            Task.WaitAll(tasks);
        }
    }
}