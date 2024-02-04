using System.IO;
using ModTek.Common.Globals;

namespace ModTekPreloader;

internal class Paths
{
    // Common paths
    internal static string ModsDirectory => CommonPaths.ModsDirectory;
    internal static string ModTekDirectory => CommonPaths.ModTekDirectory;
    internal static string DotModTekDirectory => CommonPaths.DotModTekDirectory;
    internal static string ManagedDirectory => CommonPaths.ManagedDirectory;

    // Preloader paths
    internal static readonly string GameMainAssemblyFile = Path.Combine(ManagedDirectory, "Assembly-CSharp.dll");
    internal static readonly string InjectorsDirectory = Path.Combine(ModTekDirectory, "Injectors");
    internal static readonly string PreloaderConfigFile = Path.Combine(ModTekDirectory, "ModTekPreloader.config.json");
    internal static readonly string PreloaderConfigDefaultsFile = Path.Combine(ModTekDirectory, "ModTekPreloader.config.help.json");
    internal static readonly string AssembliesOverrideDirectory = Path.Combine(ModTekDirectory, "AssembliesOverride");
    internal static readonly string ModTekBinDirectory = Path.Combine(ModTekDirectory, "bin");
    internal static readonly string HarmonyLogFile = Path.Combine(DotModTekDirectory, "HarmonyFileLog.log");
    internal static readonly string LogFile = Path.Combine(DotModTekDirectory, "ModTekPreloader.log");
    internal static readonly string LockFile = Path.Combine(DotModTekDirectory, "ModTekPreloader.lock");
    internal static readonly string AssembliesInjectedDirectory = Path.Combine(DotModTekDirectory, "AssembliesInjected");
    internal static readonly string InjectionCacheManifestFile = Path.Combine(AssembliesInjectedDirectory, "_Manifest.csv");
    internal static readonly string AssembliesShimmedDirectory = Path.Combine(DotModTekDirectory, "AssembliesShimmed");
    internal static readonly string ShimmedCacheManifestFile = Path.Combine(AssembliesShimmedDirectory, "_Manifest.csv");
    internal static readonly string AssembliesLoadedLogPath = Path.Combine(DotModTekDirectory, "assemblies_loaded.log");
    internal static readonly string AssembliesLoadedDirectory = Path.Combine(DotModTekDirectory, "AssembliesLoaded");
}