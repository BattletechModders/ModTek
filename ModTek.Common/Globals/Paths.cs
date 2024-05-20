using System;
using System.IO;

namespace ModTek.Common.Globals;

internal class Paths
{
    // Common paths
    private const string ENV_DOORSTOP_MANAGED_FOLDER_DIR = "DOORSTOP_MANAGED_FOLDER_DIR";
    internal static readonly string ManagedDirectory = Environment.GetEnvironmentVariable(ENV_DOORSTOP_MANAGED_FOLDER_DIR)
        ?? throw new Exception($"Can't find {ENV_DOORSTOP_MANAGED_FOLDER_DIR}");
    internal static readonly string BaseDirectory = Path.GetFullPath(Path.Combine(ManagedDirectory, "..", ".."));

    internal static readonly string ModsDirectory = Path.Combine(BaseDirectory, "Mods");
    internal static readonly string ModTekDirectory = Path.Combine(ModsDirectory, "ModTek");
    internal static readonly string DotModTekDirectory = Path.Combine(ModsDirectory, ".modtek");

    // Preloader & InjectorRunner paths
    internal static readonly string GameMainAssemblyFile = Path.Combine(ManagedDirectory, "Assembly-CSharp.dll");
    internal static readonly string InjectorsDirectory = Path.Combine(ModTekDirectory, "Injectors");
    internal static readonly string PreloaderConfigFile = Path.Combine(ModTekDirectory, "ModTekPreloader.config.json");
    internal static readonly string PreloaderConfigDefaultsFile = Path.Combine(ModTekDirectory, "ModTekPreloader.config.help.json");
    internal static readonly string AssembliesOverrideDirectory = Path.Combine(ModTekDirectory, "AssembliesOverride");
    internal static readonly string ModTekLibDirectory = Path.Combine(ModTekDirectory, "lib");
    internal static readonly string HarmonyLogFile = Path.Combine(DotModTekDirectory, "HarmonyFileLog.log");
    internal static readonly string PreloaderLogFile = Path.Combine(DotModTekDirectory, "ModTekPreloader.log");
    internal static readonly string InjectorRunnerLogFile = Path.Combine(DotModTekDirectory, "ModTekInjectorRunner.log");
    internal static readonly string PreloaderLockFile = Path.Combine(DotModTekDirectory, "ModTekPreloader.lock");
    internal static readonly string AssembliesInjectedDirectory = Path.Combine(DotModTekDirectory, "AssembliesInjected");
    internal static readonly string InjectionCacheManifestFile = Path.Combine(AssembliesInjectedDirectory, "_Manifest.csv");
    internal static readonly string AssembliesShimmedDirectory = Path.Combine(DotModTekDirectory, "AssembliesShimmed");
    internal static readonly string ShimmedCacheManifestFile = Path.Combine(AssembliesShimmedDirectory, "_Manifest.csv");
    internal static readonly string AssembliesLoadedLogPath = Path.Combine(DotModTekDirectory, "assemblies_loaded.log");
    internal static readonly string AssembliesLoadedDirectory = Path.Combine(DotModTekDirectory, "AssembliesLoaded");
}
