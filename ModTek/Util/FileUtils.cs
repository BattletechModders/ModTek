using System;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.RuntimeLog;
using UnityEngine;

namespace ModTek.Util
{
    internal static class FileUtils
    {
        internal static void CleanModTekTempDir(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
            {
                return;
            }

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                CleanModTekTempDir(dir);
            }

            var files = baseDir.GetFiles();
            foreach (var file in files)
            {
                if (file.Name == "ModTek.log")
                {
                    continue;
                }

                if (file.Name == "ModTek_runtime_log.txt")
                {
                    continue;
                }

                file.IsReadOnly = false;
                RLog.M.TWL(0, "delete file " + file.FullName);
                try
                {
                    file.Delete();
                }
                catch (Exception)
                {
                }
            }

            RLog.M.TWL(0, "delete directory " + baseDir.FullName);
            try
            {
                baseDir.Delete();
            }
            catch (Exception)
            {
            }
        }

        internal static string ResolvePath(string path, string rootPathToUse)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(rootPathToUse, path);
            }

            return Path.GetFullPath(path);
        }

        internal static string GetRelativePath(string path, string rootPath)
        {
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            rootPath = Path.GetFullPath(rootPath);
            if (rootPath.Last() != Path.DirectorySeparatorChar)
            {
                rootPath += Path.DirectorySeparatorChar;
            }

            var pathUri = new Uri(Path.GetFullPath(path), UriKind.Absolute);
            var rootUri = new Uri(rootPath, UriKind.Absolute);

            if (pathUri.Scheme != rootUri.Scheme)
            {
                return path;
            }

            var relativeUri = rootUri.MakeRelativeUri(pathUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (pathUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        private static readonly string[] IGNORE_LIST =
        {
            ".DS_STORE",
            "~",
            ".nomedia"
        };

        public static bool FileIsOnDenyList(string filePath)
        {
            return IGNORE_LIST.Any(x => filePath.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
        }


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
