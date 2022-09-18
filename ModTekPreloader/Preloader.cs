using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ModTekPreloader.Injector;

namespace ModTekPreloader
{
    internal class Preloader
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

        // ENTRY POINT
        internal static void Run()
        {
            var injector = new Preloader();
            injector.RestoreFromBackupAndDeleteBackup();
            injector.CleanupObsoleteFiles();
            injector.RunInjectors();
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
            Directory.CreateDirectory(Paths.AssembliesInjectedDirectory);
            string[] GetAssembliesInjectedFiles() => Directory.GetFiles(Paths.AssembliesInjectedDirectory, "*.dlL");
            foreach (var file in GetAssembliesInjectedFiles())
            {
                File.Delete(file);
            }

            var injectorAppDomain = AppDomain.CreateDomain("Injectors");
            try
            {
                var runner = (Runner)injectorAppDomain.CreateInstanceAndUnwrap(
                    typeof(Runner).Assembly.FullName,
                    // ReSharper disable once AssignNullToNotNullAttribute
                    typeof(Runner).FullName
                );
                runner.RunInjectors(Logger.Start);
            }
            finally
            {
                AppDomain.Unload(injectorAppDomain);
            }

            // to force injected assemblies to be used
            Logger.Log("Preloading injected assemblies.");
            foreach (var file in GetAssembliesInjectedFiles())
            {
                Logger.Log($"\t{Paths.GetRelativePath(file)}");
                Assembly.LoadFile(file);
            }
        }
    }
}
