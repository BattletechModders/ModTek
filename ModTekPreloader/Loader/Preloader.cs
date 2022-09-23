using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Doorstop;
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

        internal void PrepareInjection()
        {
            Logger.Log("Preloader starting");
            Paths.Print();
            SingleInstanceEnforcer.Enforce();
            RestoreFromBackupAndDeleteBackup();
            CleanupObsoleteFiles();
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

        private DynamicShimInjector _shimInjector;
        internal void RunInjectors(Entrypoint.GameAssemblyLoader assemblyLoader)
        {
            _shimInjector = new DynamicShimInjector();

            // TODO allow Harmony modifications
            // not supporting modifying Harmony assemblies during injection, hence preloading them now
            PreloadAssembliesHarmony(assemblyLoader);

            using (var injectorsRunner = new InjectorsRunner(_shimInjector))
            {
                if (!injectorsRunner.IsUpToDate)
                {
                    injectorsRunner.RunInjectors();
                    injectorsRunner.SaveToDisk();
                }
                injectorsRunner.PreloadAssembliesInjected(assemblyLoader);
            }
            PreloadAssembliesOverride(assemblyLoader);
        }

        // TODO allow Harmony modifications
        // would need to implement different AppDomains to support Harmony1, 2 and X modifications
        // meaning 3 different injection phases and in between upgrading the shims
        // all the while having to share the assembly cache between the app domains
        private void PreloadAssembliesHarmony(Entrypoint.GameAssemblyLoader assemblyLoader)
        {
            if (_shimInjector.Enabled)
            {
                _shimInjector.PreloadAssembliesHarmonyX(assemblyLoader);
                return;
            }

            {
                var path = Path.Combine(Paths.AssembliesOverrideDirectory, "0Harmony.dll");
                if (File.Exists(path))
                {
                    Logger.Log($"Preloading harmony from {Path.GetFileName(path)} into {Entrypoint.AppDomainNameUnity}.");
                    assemblyLoader.LoadFile(path);
                    return;
                }
            }

            {
                var path = Path.Combine(Paths.ManagedDirectory, "0Harmony.dll");
                Logger.Log($"Preloading harmony from {Path.GetFileName(path)} into the Unity AppDomain.");
                assemblyLoader.LoadFile(path);
            }
        }

        private void PreloadAssembliesOverride(Entrypoint.GameAssemblyLoader assemblyLoader)
        {
            Logger.Log($"Preloading override assemblies from `{Paths.GetRelativePath(Paths.AssembliesOverrideDirectory)}` into {Entrypoint.AppDomainNameUnity}:");
            foreach (var file in Directory.GetFiles(Paths.AssembliesOverrideDirectory, "*.dll").OrderBy(p => p))
            {
                Logger.Log($"\t{Path.GetFileName(file)}");
                assemblyLoader.LoadFile(file);
            }
        }

        internal bool ShouldRegisterShimInjectorPatches => _shimInjector.Enabled;

        // used by patches
        internal void InjectShimIfNecessary(ref string path)
        {
            _shimInjector.InjectShimIfNecessary(ref path);
        }
    }
}
