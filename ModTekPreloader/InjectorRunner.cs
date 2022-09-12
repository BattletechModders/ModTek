using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ModTekPreloader
{
    public class InjectorRunner : MarshalByRefObject
    {
        public void RunInjectors(DateTime loggerStart)
        {
            Logger.Setup(loggerStart);

            var paths = new Paths();
            using (var cache = new AssemblyCache())
            {
                {
                    Logger.Log($"Running {nameof(ModTekInjector)}.");
                    ModTekInjector.Inject(cache);
                }

                var parameters = new object[]
                {
                    cache
                };

                Logger.Log($"Searching injector dlls.");
                foreach (var injectorPath in Directory.GetFiles(paths.injectorsDirectory, "*.dll").OrderBy(p => p))
                {
                    Logger.Log($"Running injector {Path.GetFileName(injectorPath)}.");
                    var injector = Assembly.LoadFile(injectorPath);
                    foreach (var injectMethod in injector
                                 .GetTypes()
                                 .Where(t => t.Name == "Injector")
                                 .Select(t => t.GetMethod("Inject", BindingFlags.Public | BindingFlags.Static))
                                 .Where(m => m != null))
                    {
                        injectMethod.Invoke(null, parameters);
                        break;
                    }
                }

                cache.SaveAssembliesToDisk(paths.assembliesInjectedDirectory);
            }
        }
    }
}
