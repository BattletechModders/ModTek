using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

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

        private readonly Paths paths = new Paths();

        private void RestoreFromBackupAndDeleteBackup()
        {
            Logger.Log(nameof(RestoreFromBackupAndDeleteBackup));

            var gameDLLModTekBackupPath = paths.gameDLLPath + ".orig";
            var gameDLLPerFixBackupPath = paths.gameDLLPath + ".PerfFix.orig";

            if (InjectedChecker.IsInjected(paths.gameDLLPath)
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
            if (!File.Exists(backupPath) || InjectedChecker.IsInjected(backupPath))
            {
                return false;
            }

            File.Copy(backupPath, paths.gameDLLPath, true);
            Logger.Log($"{Path.GetFileName(backupPath)} restored to {Path.GetFileName(paths.gameDLLPath)}");
            return true;
        }

        private void CleanupObsoleteFiles()
        {
            Logger.Log(nameof(CleanupObsoleteFiles));
            foreach (var relativePathWithPlaceholder in OBSOLETE_FILES)
            {
                var path = relativePathWithPlaceholder
                    .Replace("(Mods)", paths.modsDirectory)
                    .Replace("(Managed)", paths.managedDirectory);
                File.Delete(path);
            }
        }

        private void RunInjectors()
        {
            Logger.Log(nameof(RunInjectors));
            Directory.CreateDirectory(paths.assembliesInjectedDirectory);
            string[] GetAssembliesInjectedFiles() => Directory.GetFiles(paths.assembliesInjectedDirectory, "*.dlL");
            foreach (var file in GetAssembliesInjectedFiles())
            {
                File.Delete(file);
            }

            var injectorAppDomain = AppDomain.CreateDomain("Injectors");
            try
            {
                var runner = (InjectorRunner)injectorAppDomain.CreateInstanceAndUnwrap(
                    nameof(ModTekPreloader),
                    $"{nameof(ModTekPreloader)}.{nameof(InjectorRunner)}"
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
                Logger.Log($"\t{FileUtils.GetRelativePath(file)}");
                Assembly.LoadFile(file);
            }
        }
    }
}
