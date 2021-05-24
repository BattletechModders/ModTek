using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    ModTek.FailedToLoadMods.Add(FileUtils.GetRelativePath(modDirectory, FilePaths.ModsDirectory));
                    Logger.LogException((string) $"Error: Caught exception while parsing {modDefPath}", e);
                    continue;
                }

                if (ModTek.allModDefs.ContainsKey(modDef.Name) == false)
                {
                    ModTek.allModDefs.Add(modDef.Name, modDef);
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
                    while (ModTek.allModDefs.ContainsKey(tmpname) == true);

                    modDef.Name = tmpname;
                    modDef.Enabled = false;
                    modDef.LoadFail = true;
                    modDef.FailReason = "dublicate";
                    ModTek.allModDefs.Add(modDef.Name, modDef);
                    continue;
                }

                if (!modDef.ShouldTryLoad(ModTek.ModDefs.Keys.ToList(), out var reason, out _))
                {
                    Logger.Log((string) $"Not loading {modDef.Name} because {reason}");
                    if (!modDef.IgnoreLoadFailure)
                    {
                        ModTek.FailedToLoadMods.Add(modDef.Name);
                        if (ModTek.allModDefs.ContainsKey(modDef.Name))
                        {
                            ModTek.allModDefs[modDef.Name].LoadFail = true;
                            modDef.FailReason = reason;
                        }
                    }

                    continue;
                }

                ModTek.ModDefs.Add(modDef.Name, modDef);
            }
        }

        public static void SetupModLoadOrderAndRemoveUnloadableMods()
        {
            // get a load order and remove mods that won't be loaded
            ModLoadOrder = LoadOrder.CreateLoadOrder(ModTek.ModDefs, out var notLoaded, LoadOrder.FromFile(FilePaths.LoadOrderPath));
            foreach (var modName in notLoaded)
            {
                var modDef = ModTek.ModDefs[modName];
                ModTek.ModDefs.Remove(modName);
                if (modDef.IgnoreLoadFailure)
                {
                    continue;
                }

                if (ModTek.allModDefs.ContainsKey(modName))
                {
                    ModTek.allModDefs[modName].LoadFail = true;
                    ModTek.allModDefs[modName].FailReason = $"Warning: Will not load {modName} because it's lacking a dependency or has a conflict.";
                }

                Logger.Log((string) $"Warning: Will not load {modName} because it's lacking a dependency or has a conflict.");
                ModTek.FailedToLoadMods.Add(modName);
            }
        }

        internal static List<string> ModLoadOrder;
    }
}
