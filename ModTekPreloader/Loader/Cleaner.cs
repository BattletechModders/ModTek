using System;
using System.Collections.Generic;
using System.IO;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Loader;

internal class Cleaner
{
    private static readonly List<string> OBSOLETE_FILES = new()
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

    internal static void Clean()
    {
        RestoreFromBackupAndDeleteBackup();
        CleanupObsoleteFiles();
    }

    private static void RestoreFromBackupAndDeleteBackup()
    {
        Logger.Main.Log("Find backups, restore assemblies and remove backups.");

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

    private static bool RestoreIfUnInjectedBackupFound(string backupPath)
    {
        if (!File.Exists(backupPath) || LegacyChecker.IsInjected(backupPath))
        {
            return false;
        }

        File.Copy(backupPath, Paths.GameMainAssemblyFile, true);
        Logger.Main.Log($"{Paths.GetRelativePath(Paths.GameMainAssemblyFile)} restored from {Paths.GetRelativePath(backupPath)} .");
        return true;
    }

    private static void CleanupObsoleteFiles()
    {
        Logger.Main.Log("Cleaning up obsolete files.");
        foreach (var relativePathWithPlaceholder in OBSOLETE_FILES)
        {
            var path = relativePathWithPlaceholder
                .Replace("(Mods)", Paths.ModsDirectory)
                .Replace("(Managed)", Paths.ManagedDirectory);
            File.Delete(path);
        }
    }
}