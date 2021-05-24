using System.IO;
using BattleTech;
using UnityEngine;

namespace ModTek.Misc
{
    internal static class FilePaths
    {
        internal const string MOD_JSON_NAME = "mod.json";

        private const string MODS_DIRECTORY_NAME = "Mods";
        private const string MODTEK_DIRECTORY_NAME = "ModTek";
        private const string TEMP_MODTEK_DIRECTORY_NAME = ".modtek";
        private const string CACHE_DIRECTORY_NAME = "Cache";
        private const string MERGE_CACHE_FILE_NAME = "merge_cache.json";
        private const string TYPE_CACHE_FILE_NAME = "type_cache.json";
        private const string LOG_NAME = "ModTek.log";
        private const string LOAD_ORDER_FILE_NAME = "load_order.json";
        private const string DATABASE_DIRECTORY_NAME = "Database";
        private const string MDD_FILE_NAME = "MetadataDatabase.db";
        private const string DB_CACHE_FILE_NAME = "database_cache.json";
        private const string HARMONY_SUMMARY_FILE_NAME = "harmony_summary.log";
        private const string CONFIG_FILE_NAME = "config.json";
        private const string CHANGED_FLAG_NAME = ".changed";

        internal static string ModTekDirectory { get; private set; }
        internal static string TempModTekDirectory { get; private set; }
        internal static string CacheDirectory { get; private set; }
        internal static string DatabaseDirectory { get; private set; }
        internal static string MergeCachePath { get; private set; }
        internal static string TypeCachePath { get; private set; }
        internal static string MDDBPath { get; private set; }
        internal static string ModMDDBPath { get; private set; }
        internal static string LoadOrderPath { get; private set; }
        internal static string HarmonySummaryPath { get; private set; }
        internal static string ConfigPath { get; private set; }
        internal static string ModTekSettingsPath { get; private set; }
        public static string GameDirectory { get; private set; }
        public static string ModsDirectory { get; private set; }
        public static string StreamingAssetsDirectory { get; private set; }
        internal static string DBCachePath { get; private set; }
        public static string ChangedFlagPath { get; private set; }
        internal static string DebugSettingsPath { get; } = Path.Combine(Path.Combine("data", "debug"), "settings.json");
        internal static string LogPath { get; set; }

        public static bool SetupPaths()
        {
            // if the manifest directory is null, there is something seriously wrong
            var manifestDirectory = Path.GetDirectoryName(VersionManifestUtilities.MANIFEST_FILEPATH);
            if (manifestDirectory == null)
            {
                return false;
            }

            // setup directories
            ModsDirectory = Path.GetFullPath(
                Path.Combine(
                    manifestDirectory,
                    Path.Combine(Path.Combine(Path.Combine("..", ".."), ".."), MODS_DIRECTORY_NAME)
                )
            );

            StreamingAssetsDirectory = Application.streamingAssetsPath;
            GameDirectory = Path.GetFullPath(Path.Combine(Path.Combine(StreamingAssetsDirectory, ".."), ".."));
            MDDBPath = Path.Combine(Path.Combine(StreamingAssetsDirectory, "MDD"), MDD_FILE_NAME);

            ModTekDirectory = Path.Combine(ModsDirectory, MODTEK_DIRECTORY_NAME);
            TempModTekDirectory = Path.Combine(ModsDirectory, TEMP_MODTEK_DIRECTORY_NAME);
            CacheDirectory = Path.Combine(TempModTekDirectory, CACHE_DIRECTORY_NAME);
            DatabaseDirectory = Path.Combine(TempModTekDirectory, DATABASE_DIRECTORY_NAME);

            ChangedFlagPath = Path.Combine(TempModTekDirectory, CHANGED_FLAG_NAME);
            LogPath = Path.Combine(TempModTekDirectory, LOG_NAME);
            HarmonySummaryPath = Path.Combine(TempModTekDirectory, HARMONY_SUMMARY_FILE_NAME);
            LoadOrderPath = Path.Combine(TempModTekDirectory, LOAD_ORDER_FILE_NAME);
            MergeCachePath = Path.Combine(CacheDirectory, MERGE_CACHE_FILE_NAME);
            TypeCachePath = Path.Combine(CacheDirectory, TYPE_CACHE_FILE_NAME);
            ModMDDBPath = Path.Combine(DatabaseDirectory, MDD_FILE_NAME);
            DBCachePath = Path.Combine(DatabaseDirectory, DB_CACHE_FILE_NAME);
            ConfigPath = Path.Combine(ModTekDirectory, CONFIG_FILE_NAME);
            ModTekSettingsPath = Path.Combine(ModTekDirectory, MOD_JSON_NAME);

            return true;
        }
    }
}
