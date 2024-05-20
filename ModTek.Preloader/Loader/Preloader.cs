using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTek.Common.Globals;
using ModTek.Common.Utils;
using ModTek.InjectorRunner.Injector;
using ModTek.Preloader.Harmony12X;

namespace ModTek.Preloader.Loader;

internal static class Preloader
{
    internal static void Run()
    {
        Logger.Main.Log($"Preloader v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})");
        PrintPaths();
        SingleInstanceEnforcer.Enforce();
        Cleaner.Clean();
        AssemblyTracker.Setup();

        InjectorsAppDomain.Run();

        Logger.Main.Log("Note that when preloading assemblies of the same name, the first one loaded wins.");
        DynamicShimInjector.Setup();
        PreloadAssembliesInjected();
        PreloadAssembliesOverride();
        PreloadModTek();

        AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.CodeBase)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(FileUtils.GetRelativePath)
            .LogAsList("Assemblies loaded:");
    }

    private static void PreloadAssembliesInjected()
    {
        Logger.Main.Log($"Preloading injected assemblies from `{FileUtils.GetRelativePath(Paths.AssembliesInjectedDirectory)}`:");
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

        Logger.Main.Log($"Preloading override assemblies from `{FileUtils.GetRelativePath(Paths.AssembliesOverrideDirectory)}`:");
        foreach (var file in Directory.GetFiles(Paths.AssembliesOverrideDirectory, "*.dll").OrderBy(p => p))
        {
            Logger.Main.Log($"\t{Path.GetFileName(file)}");
            Assembly.LoadFile(file);
        }
    }

    private static void PreloadModTek()
    {
        var file = Path.Combine(Paths.ModTekLibDirectory, "ModTek.dll");
        Logger.Main.Log($"Preloading ModTek from `{FileUtils.GetRelativePath(file)}`:");
        Assembly.LoadFile(file);
    }

    private static void PrintPaths()
    {
        Logger.Main.Log($"{nameof(Paths.GameMainAssemblyFile)}: {Paths.GameMainAssemblyFile}");
        Logger.Main.Log($"{nameof(Paths.ModTekDirectory)}: {Paths.ModTekDirectory}");
    }
}