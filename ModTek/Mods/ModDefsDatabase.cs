using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Logging;
using ModTek.Misc;
using ModTek.Util;

namespace ModTek.Mods
{
    internal static class ModDefsDatabase
    {
        internal static void CreateModDefs(string[] modDirectories)
        {
            // create ModDef objects for each mod.json file
            foreach (var modDirectory in modDirectories)
            {
                ModDefEx modDef;
                var modDefPath = Path.Combine(modDirectory, FilePaths.MOD_JSON_NAME);
                try
                {
                    modDef = ModDefEx.CreateFromPath(modDefPath);
                    if (modDef.Name == ModTek.MODTEK_DEF_NAME)
                    {
                        modDef = ModTek.SettingsDef;
                    }
                }
                catch (Exception e)
                {
                    FailedToLoadMods.Add(FileUtils.GetRelativePath(modDirectory, FilePaths.ModsDirectory));
                    Logger.LogException((string) $"Error: Caught exception while parsing {modDefPath}", e);
                    continue;
                }

                if (allModDefs.ContainsKey(modDef.Name) == false)
                {
                    allModDefs.Add(modDef.Name, modDef);
                }
                else
                {
                    var counter = 0;
                    var tmpname = modDef.Name;
                    do
                    {
                        ++counter;
                        tmpname = modDef.Name + "{dublicate " + counter + "}";
                    }
                    while (allModDefs.ContainsKey(tmpname) == true);

                    modDef.Name = tmpname;
                    modDef.Enabled = false;
                    modDef.LoadFail = true;
                    modDef.FailReason = "dublicate";
                    allModDefs.Add(modDef.Name, modDef);
                    continue;
                }

                if (!modDef.ShouldTryLoad(Enumerable.ToList<string>(ModDefs.Keys), out var reason, out _))
                {
                    Logger.Log((string) $"Not loading {modDef.Name} because {reason}");
                    if (!modDef.IgnoreLoadFailure)
                    {
                        FailedToLoadMods.Add(modDef.Name);
                        if (allModDefs.ContainsKey(modDef.Name))
                        {
                            allModDefs[modDef.Name].LoadFail = true;
                            modDef.FailReason = reason;
                        }
                    }

                    continue;
                }

                ModDefs.Add(modDef.Name, modDef);
            }
        }

        public static void SetupModLoadOrderAndRemoveUnloadableMods()
        {
            // get a load order and remove mods that won't be loaded
            ModLoadOrder = LoadOrder.CreateLoadOrder(ModDefs, out var notLoaded, LoadOrder.FromFile(FilePaths.LoadOrderPath));
            foreach (var modName in notLoaded)
            {
                var modDef = ModDefs[modName];
                ModDefs.Remove(modName);
                if (modDef.IgnoreLoadFailure)
                {
                    continue;
                }

                if (allModDefs.ContainsKey(modName))
                {
                    allModDefs[modName].LoadFail = true;
                    allModDefs[modName].FailReason = $"Warning: Will not load {modName} because it's lacking a dependency or has a conflict.";
                }

                Logger.Log((string) $"Warning: Will not load {modName} because it's lacking a dependency or has a conflict.");
                FailedToLoadMods.Add(modName);
            }
        }

        internal static List<string> ModLoadOrder;
        internal static Dictionary<string, ModDefEx> ModDefs = new();
        internal static Dictionary<string, ModDefEx> allModDefs = new();
        internal static HashSet<string> FailedToLoadMods { get; } = new();
        internal static VersionManifest CachedVersionManifest;
    }
}
