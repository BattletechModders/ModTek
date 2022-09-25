using System;
using System.Collections.Generic;
using System.IO;
using ModTekPreloader.Harmony12X;
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
            "(Mods)/ModTek/Newtonsoft.Json.dll",
            "(Mods)/ModTek/config.defaults.json",
        };

        private readonly DynamicShimInjector _shimInjector;
        public Preloader()
        {
            Logger.Log("Preloader starting");
            Paths.Print();
            SingleInstanceEnforcer.Enforce();
            RestoreFromBackupAndDeleteBackup();
            CleanupObsoleteFiles();
            _shimInjector = new DynamicShimInjector();
        }

        private void RestoreFromBackupAndDeleteBackup()
        {
            Logger.Log("Find backups, restore assemblies and remove backups.");

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
            Logger.Log("Cleaning up obsolete files.");
            foreach (var relativePathWithPlaceholder in OBSOLETE_FILES)
            {
                var path = relativePathWithPlaceholder
                    .Replace("(Mods)", Paths.ModsDirectory)
                    .Replace("(Managed)", Paths.ManagedDirectory);
                File.Delete(path);
            }
        }

        internal void RunInjectors()
        {
            using (var injectorsRunner = new InjectorsRunner(_shimInjector))
            {
                if (injectorsRunner.IsUpToDate)
                {
                    return;
                }

                injectorsRunner.RunInjectors();
                injectorsRunner.SaveToDisk();
            }
        }

        internal bool Harmony12XEnabled => _shimInjector.Enabled;

        // used by patches
        internal void InjectShimIfNecessary(ref string path)
        {
            _shimInjector.InjectShimIfNecessary(ref path);
        }
    }
}
