using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTekPreloader.Harmony12X;
using ModTekPreloader.Injector;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Loader;

internal static class Preloader
{
    internal static void Run()
    {
        Logger.Main.Rotate();

        Logger.Main.Log($"Preloader v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})");
        Paths.Print();
        SingleInstanceEnforcer.Enforce();
        Cleaner.Clean();
        InjectorsAppDomain.Run();

        Logger.Main.Log("Note that when preloading assemblies of the same name, the first one loaded wins.");
        if (Config.Instance.Harmony12XEnabled)
        {
            DynamicShimInjector.Setup();
        }
        PreloadAssembliesInjected();
        PreloadAssembliesOverride();
        PreloadModTek();

        AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.CodeBase)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(Paths.GetRelativePath)
            .LogAsList("Assemblies loaded:");
    }

    private static void PreloadAssembliesInjected()
    {
        Logger.Main.Log($"Preloading injected assemblies from `{Paths.GetRelativePath(Paths.AssembliesInjectedDirectory)}`:");
        foreach (var file in Directory.GetFiles(Paths.AssembliesInjectedDirectory, "*.dll").OrderBy(p => p))
        {
            Logger.Main.Log($"\t{Path.GetFileName(file)}");
            Assembly.LoadFile(file);
        }
    }

    private static void PreloadAssembliesOverride()
    {
        if (!Directory.Exists(Paths.AssembliesOverrideDirectory))
        {
            return;
        }

        Logger.Main.Log($"Preloading override assemblies from `{Paths.GetRelativePath(Paths.AssembliesOverrideDirectory)}`:");
        foreach (var file in Directory.GetFiles(Paths.AssembliesOverrideDirectory, "*.dll").OrderBy(p => p))
        {
            Logger.Main.Log($"\t{Path.GetFileName(file)}");
            Assembly.LoadFile(file);
        }
    }

    private static void PreloadModTek()
    {
        var file = Path.Combine(Paths.ModTekDirectory, "ModTek.dll");
        Logger.Main.Log($"Preloading ModTek from `{Paths.GetRelativePath(file)}`:");
        Assembly.LoadFile(file);
    }
}