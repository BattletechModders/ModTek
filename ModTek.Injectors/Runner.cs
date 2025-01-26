using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTek.Common.Globals;
using ModTek.Common.Logging;
using ModTek.Common.Utils;

namespace ModTek.Injectors;

internal class Runner : IDisposable
{
    internal static void Run()
    {
        using var injectorsRunner = new Runner();
        if (injectorsRunner.IsUpToDate)
        {
            return;
        }

        injectorsRunner.RunInjectors();
        injectorsRunner.SaveToDisk();
    }

    internal static string[] GetInjectedPaths()
    {
        return Directory.GetFiles(Paths.AssembliesInjectedDirectory, "*.dll");
    }

    private readonly AssemblyCache _assemblyCache;
    private readonly InjectionCacheManifest _injectionCacheManifest;
    private Runner()
    {
        _assemblyCache = new AssemblyCache();
        _injectionCacheManifest = new InjectionCacheManifest();
    }

    internal bool IsUpToDate => _injectionCacheManifest.IsUpToDate;

    internal void RunInjectors()
    {
        Logger.Main.Log($"Searching injector assemblies in `{FileUtils.GetRelativePath(Paths.InjectorsDirectory)}`:");
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
        Assembly injector;
        try
        {
            injector = Assembly.LoadFile(injectorPath);
        }
        catch (Exception ex)
        {
            // TODO don't catch once injector netstandard2.0 requirement is here (ModTek >=5)
            // TODO check if we are in .NET world instead of .NET Framework world
            // TODO check if that was the issue
            Logger.Main.Log("\t\tInjector assembly could not be loaded (not netstandard2.0?)" + ex);
            return;
        }
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
        using var errorLogger = new ConsoleLoggerAdapter(Logger.Main) { Prefix = $"{name} Error: " };
        using var infoLogger = new ConsoleLoggerAdapter(Logger.Main) { Prefix = $"{name}: " };

        var originalConsoleOut = Console.Out;
        var originalConsoleError = Console.Error;
        Console.SetOut(infoLogger);
        Console.SetError(errorLogger);
        try
        {
            injectMethod.Invoke(null, [_assemblyCache]);
        }
        catch (Exception ex)
        {
            Logger.Main.Log($"Injector {name} threw an exception: " + ex);
            throw;
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
            Console.SetError(originalConsoleError);
        }
    }

    public void Dispose()
    {
        _assemblyCache.Dispose();
    }
}