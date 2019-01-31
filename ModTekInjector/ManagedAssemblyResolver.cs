using System.IO;
using Mono.Cecil;

namespace ModTekInjector
{
    public class ManagedAssemblyResolver : BaseAssemblyResolver
    {
        private readonly DefaultAssemblyResolver defaultResolver;
        private readonly string managedDirectory;

        public ManagedAssemblyResolver(string managedDir)
        {
            defaultResolver = new DefaultAssemblyResolver();
            managedDirectory = managedDir;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            AssemblyDefinition assembly;
            try
            {
                assembly = defaultResolver.Resolve(name);
            }
            catch (AssemblyResolutionException)
            {
                var assemblyPath = Path.Combine(managedDirectory, name.Name + ".dll");

                if (!File.Exists(assemblyPath))
                    throw;

                assembly = ModuleDefinition.ReadModule(assemblyPath).Assembly;
            }

            return assembly;
        }
    }
}
