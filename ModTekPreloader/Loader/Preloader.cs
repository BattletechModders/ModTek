using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Doorstop;
using ModTekPreloader.Injector;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Loader
{
    internal class Preloader : MarshalByRefObject
    {
        private static readonly List<string> OBSOLETE_FILES = new List<string>
        {
            "(Managed)/BattleTechModLoader.dll",
            "(Managed)/BattleTechModLoaderInjector.exe",
            "(Managed)/Mono.Cecil.dll",
            "(Managed)/rt-factions.zip",
            "(Mods)/ModTek.dll",
            "(Mods)/modtekassetbundle",
            "(Mods)/BTModLoader.log",
            "(Mods)/ModTek/Newtonsoft.Json.dll"
        };

        internal void Run(Entrypoint.GameAssemblyLoader loader)
        {
            Logger.Log("Preloader starting");
            Paths.Print();
            SingleInstanceEnforcer.Enforce();
            RestoreFromBackupAndDeleteBackup();
            CleanupObsoleteFiles();
            RunInjectors();
            PreloadAssemblies(loader);
            Logger.Log("Preloader finished");
        }

        private void RestoreFromBackupAndDeleteBackup()
        {
            Logger.Log(nameof(RestoreFromBackupAndDeleteBackup));

            var gameDLLModTekBackupPath = Paths.GameMainAssemblyFile + ".orig";
            var gameDLLPerFixBackupPath = Paths.GameMainAssemblyFile + ".PerfFix.orig";

            if (LegacyChecker.IsInjected(Paths.GameMainAssemblyFile)
                && !RestoreIfUnInjectedBackupFound(gameDLLModTekBackupPath)
                && !RestoreIfUnInjectedBackupFound(gameDLLPerFixBackupPath))
            {
                throw new Exception("No un-injected backup found, please verify/reset game files");
            }

            File.Delete(gameDLLModTekBackupPath);
            File.Delete(gameDLLPerFixBackupPath);
        }

        private bool RestoreIfUnInjectedBackupFound(string backupPath)
        {
            if (!File.Exists(backupPath) || LegacyChecker.IsInjected(backupPath))
            {
                return false;
            }

            File.Copy(backupPath, Paths.GameMainAssemblyFile, true);
            Logger.Log($"{Paths.GetRelativePath(Paths.GameMainAssemblyFile)} restored from {Paths.GetRelativePath(backupPath)} .");
            return true;
        }

        private void CleanupObsoleteFiles()
        {
            Logger.Log(nameof(CleanupObsoleteFiles));
            foreach (var relativePathWithPlaceholder in OBSOLETE_FILES)
            {
                var path = relativePathWithPlaceholder
                    .Replace("(Mods)", Paths.ModsDirectory)
                    .Replace("(Managed)", Paths.ManagedDirectory);
                File.Delete(path);
            }
        }

        private void RunInjectors()
        {
            Logger.Log(nameof(RunInjectors));
            InjectorsRunner.RunInjectors();
        }

        private void PreloadAssemblies(Entrypoint.GameAssemblyLoader loader)
        {
            // to force injected assemblies to be used
            Logger.Log($"Preloading injected assemblies from `{Paths.GetRelativePath(Paths.AssembliesInjectedDirectory)}`:");
            foreach (var file in Directory.GetFiles(Paths.AssembliesInjectedDirectory, "*.dll").OrderBy(p => p))
            {
                Logger.Log($"\t{Path.GetFileName(file)}");
                loader.LoadFile(file);
            }
        }
    }
}
