using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Injector;

internal class InjectorsRunner : IDisposable
{
    private readonly AssemblyCache _assemblyCache;
    private readonly InjectionCacheManifest _injectionCacheManifest;
    internal InjectorsRunner()
    {
        _assemblyCache = new AssemblyCache();
        _injectionCacheManifest = new InjectionCacheManifest();
    }

    internal bool IsUpToDate => _injectionCacheManifest.IsUpToDate;

    internal void RunInjectors()
    {
        Logger.Main.Log($"Searching injector assemblies in `{Paths.GetRelativePath(Paths.InjectorsDirectory)}`:");
        foreach (var injectorPath in Directory.GetFiles(Paths.InjectorsDirectory, "*.dll").OrderBy(p => p))
        {
            SearchInjectorEntrypointAndInvoke(injectorPath);
        }
    }

    internal void SaveToDisk()
    {
        _assemblyCache.SaveAssembliesToDisk();
        _injectionCacheManifest.RefreshAndSave();
    }

    private void SearchInjectorEntrypointAndInvoke(string injectorPath)
    {
        Logger.Main.Log($"\t{Path.GetFileName(injectorPath)}");
        var injector = Assembly.LoadFile(injectorPath);
        foreach (var injectMethod in injector
                     .GetTypes()
                     .Where(t => t.Name == "Injector")
                     .Select(t => t.GetMethod("Inject", BindingFlags.Public | BindingFlags.Static))
                     .Where(m => m != null))
        {
            var name = injector.GetName().Name;

            InvokeInjector(name, injectMethod);
            break;
        }
    }

    private void InvokeInjector(string name, MethodInfo injectMethod)
    {
        using (var errorLogger = new ConsoleLoggerAdapter { Prefix = $"{name} Error: " })
        using (var infoLogger = new ConsoleLoggerAdapter { Prefix = $"{name}: " })
        {
            var originalConsoleOut = Console.Out;
            var originalConsoleError = Console.Error;
            Console.SetOut(infoLogger);
            Console.SetError(errorLogger);
            try
            {
                injectMethod.Invoke(null, new object[] { _assemblyCache });
            }
            finally
            {
                Console.SetOut(originalConsoleOut);
                Console.SetError(originalConsoleError);
            }
        }
    }

    public void Dispose()
    {
        _assemblyCache.Dispose();
    }
}