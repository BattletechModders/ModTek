using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Logging;
using ModTek.Misc;
using ModTek.UI;
using ModTek.Util;

namespace ModTek.Mods
{
    internal static class ModDefsDatabase
    {
        internal static List<string> ModLoadOrder;
        internal static Dictionary<string, ModDefEx> ModDefs = new();
        internal static Dictionary<string, ModDefEx> allModDefs = new();
        internal static HashSet<string> FailedToLoadMods { get; } = new();
        internal static VersionManifest CachedVersionManifest;

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
                    Logger.LogException($"Error: Caught exception while parsing {modDefPath}", e);
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
                    while (allModDefs.ContainsKey(tmpname));

                    modDef.Name = tmpname;
                    modDef.Enabled = false;
                    modDef.LoadFail = true;
                    modDef.FailReason = "dublicate";
                    allModDefs.Add(modDef.Name, modDef);
                    continue;
                }

                if (!modDef.ShouldTryLoad(ModDefs.Keys.ToList(), out var reason, out _))
                {
                    Logger.Log($"Not loading {modDef.Name} because {reason}");
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

                Logger.Log($"Warning: Will not load {modName} because it's lacking a dependency or has a conflict.");
                FailedToLoadMods.Add(modName);
            }
        }

        // TODO is this needed?
        internal static IEnumerator<ProgressReport> GatherDependencyTreeLoop()
        {
            yield return new ProgressReport(0, "Gathering dependencies trees", "");
            if (allModDefs.Count == 0)
            {
                yield break;
            }

            var progress = 0;
            foreach (var mod in allModDefs)
            {
                ++progress;
                foreach (var depname in mod.Value.DependsOn)
                {
                    if (allModDefs.ContainsKey(depname))
                    {
                        if (allModDefs[depname].DependsOnMe.Contains(mod.Value) == false)
                        {
                            allModDefs[depname].DependsOnMe.Add(mod.Value);
                        }
                    }
                }
            }

            yield return new ProgressReport(1 / 3f, "Gather depends on me", string.Empty, true);
            progress = 0;
            foreach (var mod in allModDefs)
            {
                ++progress;
                mod.Value.GatherAffectingOfflineRec();
            }

            yield return new ProgressReport(2 / 3f, "Gather disable influence tree", string.Empty, true);
            progress = 0;
            foreach (var mod in allModDefs)
            {
                ++progress;
                mod.Value.GatherAffectingOnline();
            }

            yield return new ProgressReport(1, "Gather enable influence tree", string.Empty, true);
            Logger.Log("FAIL LIST:");
            foreach (var mod in allModDefs.Values)
            {
                if (mod.Enabled == false)
                {
                    continue;
                }

                ;
                if (mod.LoadFail == false)
                {
                    continue;
                }

                Logger.Log($"\t{mod.Name} fail {mod.FailReason}");
                foreach (var dmod in mod.AffectingOnline)
                {
                    var state = dmod.Key.Enabled && dmod.Key.LoadFail == false;
                    if (state != dmod.Value)
                    {
                        Logger.Log($"\t\tdepends on {dmod.Key.Name} should be loaded:{dmod.Value} but it is not cause enabled:{dmod.Key.Enabled} and fail:{dmod.Key.LoadFail} due to {dmod.Key.FailReason}");
                    }
                }

                foreach (var deps in mod.DependsOn)
                {
                    if (allModDefs.ContainsKey(deps) == false)
                    {
                        Logger.Log($"\t\tdepends on {deps} but abcent");
                    }
                }
            }
        }

        internal static List<ModDefEx> ModsInLoadOrder()
        {
            var mods = new List<ModDefEx>();
            foreach (var modname in ModLoadOrder)
            {
                if (!ModDefs.ContainsKey(modname))
                {
                    continue;
                }

                mods.Add(ModDefs[modname]);
            }
            return mods;
        }
    }
}
