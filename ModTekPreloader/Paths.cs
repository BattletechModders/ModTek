using System;
using System.IO;
using ModTekPreloader.Logging;

namespace ModTekPreloader
{
    internal class Paths
    {
        private const string ENV_DOORSTOP_MANAGED_FOLDER_DIR = "DOORSTOP_MANAGED_FOLDER_DIR";
        internal static readonly string ManagedDirectory = Environment.GetEnvironmentVariable(ENV_DOORSTOP_MANAGED_FOLDER_DIR)
            ?? throw new Exception($"Can't find {ENV_DOORSTOP_MANAGED_FOLDER_DIR}");
        internal static readonly string GameMainAssemblyFile = Path.Combine(ManagedDirectory, "Assembly-CSharp.dll");

        private static readonly string GameExecutableDirectory = Directory.GetCurrentDirectory();
        internal static readonly string ModsDirectory = Path.Combine(GameExecutableDirectory, "Mods");

        internal static readonly string ModTekDirectory = Path.Combine(ModsDirectory, "ModTek");
        internal static readonly string InjectorsDirectory = Path.Combine(ModTekDirectory, "Injectors");
        internal static readonly string PreloaderConfigFile = Path.Combine(ModTekDirectory, "ModTekPreloader.config.json");
        internal static readonly string PreloaderConfigDefaultsFile = Path.Combine(ModTekDirectory, "ModTekPreloader.config.help.json");
        internal static readonly string AssembliesOverrideDirectory = Path.Combine(ModTekDirectory, "AssembliesOverride");

        private static readonly string DotModTekDirectory = Path.Combine(ModsDirectory, ".modtek");
        internal static readonly string LogFile = Path.Combine(DotModTekDirectory, "ModTekPreloader.log");
        internal static readonly string LockFile = Path.Combine(DotModTekDirectory, ".lock");
        internal static readonly string CacheManifestFile = Path.Combine(DotModTekDirectory, "ModTekPreloaderCacheManifest.csv");
        internal static readonly string AssembliesInjectedDirectory = Path.Combine(DotModTekDirectory, "AssembliesInjected");
        internal static readonly string AssembliesPublicizedDirectory = Path.Combine(DotModTekDirectory, "AssembliesPublicized");

        internal static void CreateDirectoryForFile(string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? throw new Exception($"Could not find directory for {filePath}"));
        }

        internal static string GetRelativePath(string path)
        {
            try
            {
                return new Uri(Directory.GetCurrentDirectory()).MakeRelativeUri(new Uri(path)).ToString();
            }
            catch
            {
                return path;
            }
        }

        internal static void RotatePath(string path, int backups)
        {
            for (var i = backups - 1; i >= 0; i--)
            {
                var pathCurrent = path + (i == 0 ? "" : "." + i);
                var pathNext = path + "." + (i + 1);
                File.Delete(pathNext);
                if (File.Exists(pathCurrent))
                {
                    File.Move(pathCurrent, pathNext);
                }
            }
        }

        internal static void SetupCleanDirectory(string path, bool recursive = false)
        {
            var di = new DirectoryInfo(path);
            if (di.Exists)
            {
                foreach (var file in di.GetFiles())
                {
                    file.Delete();
                }
                if (recursive)
                {
                    foreach (var dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }
            }
            else
            {
                di.Create();
            }
        }

        internal static void Print()
        {
            Logger.Log($"{nameof(GameMainAssemblyFile)}: {GameMainAssemblyFile}");
            Logger.Log($"{nameof(ModTekDirectory)}: {ModTekDirectory}");
        }
    }
}
