using System;
using System.IO;
using UnityEngine;

namespace ModTek.Misc
{
    internal static class FilePaths
    {
        internal const string MOD_JSON_NAME = "mod.json";

        private const string MDD_FILE_NAME = "MetadataDatabase.db";

        private const string ENV_DOORSTOP_MANAGED_FOLDER_DIR = "DOORSTOP_MANAGED_FOLDER_DIR";
        internal static readonly string ManagedDirectory = Environment.GetEnvironmentVariable(ENV_DOORSTOP_MANAGED_FOLDER_DIR)
            ?? throw new Exception($"Can't find {ENV_DOORSTOP_MANAGED_FOLDER_DIR}");
        internal static readonly string BaseDirectory = Path.GetFullPath(Path.Combine(ManagedDirectory, "..", ".."));

        internal static readonly string StreamingAssetsDirectory = Application.streamingAssetsPath;
        internal static readonly string MDDBPath = Path.Combine(StreamingAssetsDirectory, "MDD", MDD_FILE_NAME);
        internal static readonly string ModsDirectory = Path.Combine(BaseDirectory, "Mods");
        internal static readonly string ModTekDirectory = Path.Combine(ModsDirectory, "ModTek");
        internal static readonly string TempModTekDirectory = Path.Combine(ModsDirectory, ".modtek");
        internal static readonly string MergeCacheDirectory = Path.Combine(TempModTekDirectory, "Cache");
        internal static readonly string MDDBCacheDirectory = Path.Combine(TempModTekDirectory, "Database");
        internal static readonly string ModMDDBPath = Path.Combine(MDDBCacheDirectory, MDD_FILE_NAME);
        internal static readonly string LoadOrderPath = Path.Combine(TempModTekDirectory, "load_order.json");
        internal static readonly string HarmonySummaryPath = Path.Combine(TempModTekDirectory, "harmony_summary.log");
        internal static readonly string AssembliesLoadedLogPath = Path.Combine(TempModTekDirectory, "assemblies_loaded.log");
        internal static readonly string ModTekSettingsPath = Path.Combine(ModTekDirectory, MOD_JSON_NAME);
        internal static readonly string AssetBundlesDirectory = Path.Combine(StreamingAssetsDirectory, "data", "assetbundles");
        internal static readonly string LogPath = Path.Combine(TempModTekDirectory, "ModTek.log");
    }
}
