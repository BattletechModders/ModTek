using System.IO;
using ModTek.Common.Globals;
using UnityEngine;

namespace ModTek.Misc;

internal static class FilePaths
{
    internal const string MOD_JSON_NAME = "mod.json";
    private const string MDD_FILE_NAME = "MetadataDatabase.db";

    // Common paths
    internal static string ModsDirectory => CommonPaths.ModsDirectory;
    internal static string ModTekDirectory => CommonPaths.ModTekDirectory;
    internal static string TempModTekDirectory => CommonPaths.DotModTekDirectory;

    // ModTek paths
    internal static readonly string StreamingAssetsDirectory = Application.streamingAssetsPath;
    internal static readonly string MDDBPath = Path.Combine(StreamingAssetsDirectory, "MDD", MDD_FILE_NAME);
    internal static readonly string AssetBundlesDirectory = Path.Combine(StreamingAssetsDirectory, "data", "assetbundles");

    internal static readonly string MergeCacheDirectory = Path.Combine(TempModTekDirectory, "Cache");
    internal static readonly string MDDBCacheDirectory = Path.Combine(TempModTekDirectory, "Database");
    internal static readonly string ModMDDBPath = Path.Combine(MDDBCacheDirectory, MDD_FILE_NAME);
    internal static readonly string LoadOrderPath = Path.Combine(TempModTekDirectory, "load_order.json");
    internal static readonly string ModTekModJsonPath = Path.Combine(ModTekDirectory, MOD_JSON_NAME);
    internal static readonly string LogPath = Path.Combine(TempModTekDirectory, "ModTek.log");
}