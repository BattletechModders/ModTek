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
            if (File.Exists(paths.gameDLLBackupPath))
            {
                Restore();
            }
            else
            {
                // read the assembly for game version and injected status
                bool injected;
                using (var game = ModuleDefinition.ReadModule(paths.gameDLLPath))
                {
                    injected = InjectedChecker.IsInjected(game);
                }
                if (injected)
                {
                    Restore();
                }
            }

            File.Delete(paths.gameDLLBackupPath);
        }

        private void Restore()
        {
            if (!File.Exists(paths.gameDLLBackupPath))
                throw new FileNotFoundException(paths.gameDLLBackupPath);

            using (var backup = ModuleDefinition.ReadModule(paths.gameDLLBackupPath))
            {
                if (InjectedChecker.IsInjected(backup))
                {
                    throw new Exception("Backups are injected and not originals, please verify/reset game files");
                }
            }

            File.Copy(paths.gameDLLBackupPath, paths.gameDLLPath, true);
            Logger.Log($"{Path.GetFileName(paths.gameDLLBackupPath)} restored to {Path.GetFileName(paths.gameDLLPath)}");
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
