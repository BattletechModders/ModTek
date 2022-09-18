using System;
using System.IO;
using BattleTech;
using ModTek.Util;
using UnityEngine;

namespace ModTek.Misc
{
    internal static class FilePaths
    {
        internal const string MOD_JSON_NAME = "mod.json";

        internal const string MODTEK_DIRECTORY_NAME = "ModTek";

        private const string TEMP_MODTEK_DIRECTORY_NAME = ".modtek";
        private const string CACHE_DIRECTORY_NAME = "Cache";
        private const string LOG_NAME = "ModTek.log";
        private const string LOAD_ORDER_FILE_NAME = "load_order.json";
        private const string DATABASE_DIRECTORY_NAME = "Database";
        private const string MDD_FILE_NAME = "MetadataDatabase.db";
        private const string HARMONY_SUMMARY_FILE_NAME = "harmony_summary.log";
        private const string PROFILING_SUMMARY_FILE_NAME = "profiling_summary.log";

        internal static string ModTekDirectory { get; private set; }
        internal static string TempModTekDirectory { get; private set; }
        internal static string MergeCacheDirectory { get; private set; }
        internal static string MDDBCacheDirectory { get; private set; }
        internal static string MDDBPath { get; private set; }
        internal static string ModMDDBPath { get; private set; }
        internal static string LoadOrderPath { get; private set; }
        internal static string HarmonySummaryPath { get; private set; }
        internal static string ProfilingSummaryPath { get; private set; }
        internal static string ModTekSettingsPath { get; private set; }
        internal static string GameDirectory { get; private set; }
        internal static string ModsDirectory { get; private set; }
        internal static string StreamingAssetsDirectory { get; private set; }
        internal static string StreamingAssetsDirectoryName { get; private set; }
        internal static string AssetBundlesDirectory { get; private set; }
        internal static string LogPath { get; set; }
        internal static string LogPathRelativeToGameDirectory => FileUtils.GetRelativePath(GameDirectory, LogPath);

        internal static void SetupPaths()
        {
            // setup directories
            ModsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Mods");

            StreamingAssetsDirectory = Application.streamingAssetsPath;

            StreamingAssetsDirectoryName = Path.GetFileName(StreamingAssetsDirectory);
            AssetBundlesDirectory = Path.Combine(StreamingAssetsDirectory, "data/assetbundles");
            GameDirectory = Path.GetFullPath(Path.Combine(Path.Combine(StreamingAssetsDirectory, ".."), ".."));
            MDDBPath = Path.Combine(Path.Combine(StreamingAssetsDirectory, "MDD"), MDD_FILE_NAME);

            ModTekDirectory = Path.Combine(ModsDirectory, MODTEK_DIRECTORY_NAME);
            TempModTekDirectory = Path.Combine(ModsDirectory, TEMP_MODTEK_DIRECTORY_NAME);
            MergeCacheDirectory = Path.Combine(TempModTekDirectory, CACHE_DIRECTORY_NAME);
            MDDBCacheDirectory = Path.Combine(TempModTekDirectory, DATABASE_DIRECTORY_NAME);

            LogPath = Path.Combine(TempModTekDirectory, LOG_NAME);
            HarmonySummaryPath = Path.Combine(TempModTekDirectory, HARMONY_SUMMARY_FILE_NAME);
            ProfilingSummaryPath = Path.Combine(TempModTekDirectory, PROFILING_SUMMARY_FILE_NAME);
            LoadOrderPath = Path.Combine(TempModTekDirectory, LOAD_ORDER_FILE_NAME);
            ModMDDBPath = Path.Combine(MDDBCacheDirectory, MDD_FILE_NAME);
            ModTekSettingsPath = Path.Combine(ModTekDirectory, MOD_JSON_NAME);

        }
    }
}
