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
        private static readonly List<string> MANAGED_DIRECTORY_SEEK_LIST = new List<string>
        {
            "BattleTech_Data/Managed",
            "Data/Managed"
        };

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

        private const string GAME_DLL_FILE_NAME = "Assembly-CSharp.dll";

        // ENTRY POINT
        internal static void Run(string gameDirectory)
        {
            var injector = new Preloader(gameDirectory);
            injector.RestoreFromBackupAndDeleteBackup();
            injector.CleanupObsoleteFiles();
            injector.RunInjectors();
        }

        private readonly string managedDirectory;
        private readonly string gameDLLPath;
        private readonly string gameDLLBackupPath;
        private readonly string modsDirectory;
        private readonly string injectorsDirectory;
        private readonly string assembliesInjectedDirectory;

        private Preloader(string gameDirectory)
        {
            managedDirectory = FindManagedDirectory(gameDirectory);
            gameDLLPath = Path.Combine(managedDirectory, GAME_DLL_FILE_NAME);
            gameDLLBackupPath = Path.Combine(managedDirectory, GAME_DLL_FILE_NAME + ".orig");
            modsDirectory = Path.Combine(gameDirectory, "Mods");
            var modTekDirectory = Path.Combine(modsDirectory, "ModTek");
            injectorsDirectory = Path.Combine(modTekDirectory, "Injectors");
            var dotModTekDirectory = Path.Combine(modsDirectory, ".modtek");
            assembliesInjectedDirectory = Path.Combine(dotModTekDirectory, "AssembliesInjected");
            ExistsOrThrow(managedDirectory, gameDLLPath, modsDirectory);
        }

        private static string FindManagedDirectory(string gameDirectory)
        {
            foreach (var candidateRelativePath in MANAGED_DIRECTORY_SEEK_LIST)
            {
                var candidateAbsolutePath = Path.GetFullPath(Path.Combine(gameDirectory, candidateRelativePath));
                var seekPath = Path.Combine(candidateAbsolutePath, GAME_DLL_FILE_NAME);
                if (File.Exists(seekPath))
                {
                    return candidateAbsolutePath;
                }
            }
            throw new Exception("Can't find managed directory");
        }

        private static void ExistsOrThrow(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (!Directory.Exists(path) && !File.Exists(path))
                {
                    throw new Exception($"Can't find {path}");
                }
            }
        }

        private void RestoreFromBackupAndDeleteBackup()
        {
            if (File.Exists(gameDLLBackupPath))
            {
                Restore();
            }
            else
            {
                // read the assembly for game version and injected status
                bool injected;
                using (var game = ModuleDefinition.ReadModule(gameDLLPath))
                {
                    injected = InjectedChecker.IsInjected(game);
                }
                if (injected)
                {
                    Restore();
                }
            }

            File.Delete(gameDLLBackupPath);
        }

        private void Restore()
        {
            if (!File.Exists(gameDLLBackupPath))
                throw new FileNotFoundException(gameDLLBackupPath);

            using (var backup = ModuleDefinition.ReadModule(gameDLLBackupPath))
            {
                if (InjectedChecker.IsInjected(backup))
                {
                    throw new Exception("Backups are injected and not originals, please verify/reset game files");
                }
            }

            File.Copy(gameDLLBackupPath, gameDLLPath, true);
            Logger.Log($"{Path.GetFileName(gameDLLBackupPath)} restored to {Path.GetFileName(gameDLLPath)}");
        }

        private void CleanupObsoleteFiles()
        {
            foreach (var relativePathWithPlaceholder in OBSOLETE_FILES)
            {
                var path = relativePathWithPlaceholder
                    .Replace("(Mods)", modsDirectory)
                    .Replace("(Managed)", managedDirectory);
                File.Delete(path);
            }
        }

        private void RunInjectors()
        {
            Directory.CreateDirectory(assembliesInjectedDirectory);
            foreach (var file in Directory.GetFiles(assembliesInjectedDirectory, "*.dlL"))
            {
                File.Delete(file);
            }

            using (var cache = new AssemblyCache())
            {
                ModTekInjector.Inject(cache);

                var parameters = new object[] { cache };

                foreach (var injectorPath in Directory.GetFiles(injectorsDirectory, "*.dll"))
                {
                    // Injector
                    var injector = Assembly.LoadFile(injectorPath);
                    foreach (var injectMethod in injector
                                 .GetTypes()
                                 .Where(t => t.Name == "Injector")
                                 .Select(t => t.GetMethod("Inject", BindingFlags.Public | BindingFlags.Static))
                                 .Where(m => m != null))
                    {
                        injectMethod.Invoke(null, parameters);
                    }
                }

                cache.DumpAssembliesToDiskThenLoadFromFile(assembliesInjectedDirectory);
            }
        }
    }
}
