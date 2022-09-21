using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Injector
{
    internal static class InjectorsRunner
    {
        internal static void RunInjectors()
        {
            var cacheManifest = InjectionCacheManifest.Load();
            if (cacheManifest.IsUpToDate)
            {
                Logger.Log($"Skipping injection, cache manifest at `{Paths.GetRelativePath(Paths.InjectionCacheManifestFile)}` is up to date.");
                return;
            }
            using (var assemblyCache = new AssemblyCache())
            {
                var parameters = new object[]
                {
                    assemblyCache
                };

                var originalConsoleOut = Console.Out;
                var originalConsoleError = Console.Error;

                Logger.Log($"Searching injector dlls in `{Paths.GetRelativePath(Paths.InjectorsDirectory)}`:");
                foreach (var injectorPath in Directory.GetFiles(Paths.InjectorsDirectory, "*.dll").OrderBy(p => p))
                {
                    Logger.Log($"\t{Path.GetFileName(injectorPath)}");
                    var injector = Assembly.LoadFile(injectorPath);
                    foreach (var injectMethod in injector
                                 .GetTypes()
                                 .Where(t => t.Name == "Injector")
                                 .Select(t => t.GetMethod("Inject", BindingFlags.Public | BindingFlags.Static))
                                 .Where(m => m != null))
                    {
                        var name = injector.GetName().Name;

                        using (var errorLogger = new ConsoleLoggerAdapter { Prefix = $"{name} Error: " })
                        using (var infoLogger = new ConsoleLoggerAdapter { Prefix = $"{name}: " })
                        {
                            Console.SetOut(infoLogger);
                            Console.SetError(errorLogger);
                            try
                            {
                                injectMethod.Invoke(null, parameters);
                            }
                            finally
                            {
                                Console.SetOut(originalConsoleOut);
                                Console.SetError(originalConsoleError);
                            }
                        }
                        break;
                    }
                }

                assemblyCache.SaveAssembliesToDisk();
                assemblyCache.SaveAssembliesPublicizedToDisk();
            }
            cacheManifest.Save();
        }
    }
}
