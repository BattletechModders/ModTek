using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTek.Misc;
using ModTek.UI;
using ModTek.Util;

namespace ModTek.Features.Manifest.Mods;

internal static class ModDefsDatabase
{
    private static List<string> ModLoadOrder;

    internal static readonly Dictionary<string, ModDefEx> ModDefs = new Dictionary<string, ModDefEx>();
    internal static readonly Dictionary<string, ModDefEx> allModDefs = new Dictionary<string, ModDefEx>();
    internal static HashSet<string> FailedToLoadMods { get; } = new HashSet<string>();
    //internal static Dictionary<string, string> existingAssemblies { get; set; } = new Dictionary<string, string>();
    internal static IEnumerator<ProgressReport> GatherDependencyTreeLoop()
    {
        var sliderText = "Gathering dependencies trees";
        yield return new ProgressReport(0, sliderText, "");
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

        yield return new ProgressReport(1 / 3f, sliderText, "Gather depends on me", true);
        progress = 0;
        foreach (var mod in allModDefs)
        {
            ++progress;
            mod.Value.GatherAffectingOfflineRec();
        }

        yield return new ProgressReport(2 / 3f, sliderText, "Gather disable influence tree", true);
        progress = 0;
        foreach (var mod in allModDefs)
        {
            ++progress;
            mod.Value.GatherAffectingOnline();
        }

        yield return new ProgressReport(1, sliderText, "Gather enable influence tree", true);
        Log.Main.Debug?.Log("FAIL LIST:");
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

            Log.Main.Debug?.Log($"\t{mod.QuotedName} fail {mod.FailReason}");
            foreach (var dmod in mod.AffectingOnline)
            {
                var state = dmod.Key.Enabled && dmod.Key.LoadFail == false;
                if (state != dmod.Value)
                {
                    Log.Main.Debug?.Log($"\t\tdepends on {dmod.Key.QuotedName} should be loaded:{dmod.Value} but it is not cause enabled:{dmod.Key.Enabled} and fail:{dmod.Key.LoadFail} due to {dmod.Key.FailReason}");
                }
            }

            foreach (var deps in mod.DependsOn)
            {
                if (allModDefs.ContainsKey(deps) == false)
                {
                    Log.Main.Debug?.Log($"\t\tdepends on {deps} but abcent");
                }
            }
        }
    }

    internal static List<ModDefEx> ModsInLoadOrder()
    {
        return ModLoadOrder
            .Select(name => ModDefs.TryGetValue(name, out var modDef) ? modDef : null)
            .Where(modDef => modDef != null)
            .ToList();
    }

    internal static IEnumerator<ProgressReport> InitModsLoop()
    {
        var sliderText = "Initializing Mods";
        yield return new ProgressReport(1, sliderText, "");

        string[] modJsons;
        if (ModTek.Config.SearchModsInSubDirectories)
        {
            modJsons = Directory.GetFiles(FilePaths.ModsDirectory, FilePaths.MOD_JSON_NAME, SearchOption.AllDirectories);
        }
        else
        {
            modJsons = Directory.GetDirectories(FilePaths.ModsDirectory)
                .Select(d => Path.Combine(d, FilePaths.MOD_JSON_NAME))
                .Where(File.Exists)
                .ToArray();
        }

        if (modJsons.Length == 0)
        {
            Log.Main.Info?.Log("No ModTek-compatible mods found.");
            yield break;
        }

        CreateModDefs(modJsons);
        SetupModLoadOrderAndRemoveUnloadableMods();

        // try loading each mod
        var countCurrent = 0;
        var countMax = (float) ModLoadOrder.Count;
        Log.Main.Info?.Log("");
        foreach (var modName in ModLoadOrder)
        {
            var modDef = ModDefs[modName];

            if (ModTek.Config.BlockedMods.Contains(modName))
            {
                OnModLoadFailure(modName, $"Warning: Mod {modName} is blocked and won't be loaded!", canIgnoreFailure: false);
                continue;
            }

            if (modDef.DependsOn.Intersect(FailedToLoadMods).Any())
            {
                OnModLoadFailure(modName, $"Warning: Skipping load of {modName} because one of its dependencies failed to load.");
                continue;
            }

            yield return new ProgressReport(countCurrent++/countMax, sliderText, $"{modDef.Name}\n{modDef.Version}", true);

            // expand the manifest (parses all JSON as well)
            if (!CheckManifest(modDef))
            {
                OnModLoadFailure(modName, "Failures in manifest");
                continue;
            }

            try
            {
                if (!ModDefExLoading.LoadMod(modDef, out var reason))
                {
                    OnModLoadFailure(modName, reason);
                }
            }
            catch (Exception e)
            {
                OnModLoadFailure(modName, $"Error: Tried to load mod: {modName}, but something went wrong. Make sure all of your JSON is correct!", e);
            }
        }
    }

    private static bool CheckManifest(ModDefEx modDef)
    {
        if (modDef.Manifest.Any(entry => string.IsNullOrEmpty(entry.Path)))
        {
            Log.Main.Info?.Log($"\tError: {modDef.QuotedName} has a manifest entry that is missing its path! Aborting load.");
            return false;
        }

        // Logger.Log($"\tError: {modDef.Name} has a Prefab '{entry.Id}' that's referencing an AssetBundle '{entry.AssetBundleName}' that hasn't been loaded. Put the assetbundle first in the manifest!");
        // Logger.Log($"\tError: {modDef.Name} has a manifest entry that has a type '{modEntry.Type}' that doesn't match an existing type and isn't declared in CustomResourceTypes");
        // Logger.Log($"\tWarning: Manifest specifies file/directory of {modEntry.Type} at path {modEntry.Path}, but it's not there. Continuing to load.");
        return true;
    }

    private static void CreateModDefs(string[] modJsons)
    {
        // create ModDef objects for each mod.json file
        var forceEnableMods = new HashSet<string>();
        foreach (var modDefPath in modJsons)
        {
            ModDefEx modDef;
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
                var probableModName = Path.GetFileName(Path.GetDirectoryName(modDefPath));
                FailedToLoadMods.Add(probableModName);
                Log.Main.Info?.Log($"Error: Caught exception while parsing {modDefPath}", e);
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
                OnModLoadFailure(modDef.Name, $"Not loading {modDef.Name} because {reason}");
                continue;
            }
            ModDefs.Add(modDef.Name, modDef);
            foreach (var fe_mod in modDef.forceEnableMods) {
                forceEnableMods.Add(fe_mod);
            }
        }
        foreach(var fe_mod in forceEnableMods)
        {
            if(allModDefs.TryGetValue(fe_mod, out var modToEnable))
            {
                if (ModDefs.ContainsKey(fe_mod)) { continue; }
                modToEnable.Enabled = true;
                if (!modToEnable.ShouldTryLoad(ModDefs.Keys.ToList(), out var reason, out _))
                {
                    OnModLoadFailure(modToEnable.Name, $"Not loading {modToEnable.Name} because {reason}");
                    continue;
                }
                ModDefs.Add(modToEnable.Name, modToEnable);
            }
        }
    }

    private static void SetupModLoadOrderAndRemoveUnloadableMods()
    {
        // get a load order and remove mods that won't be loaded
        ModLoadOrder = LoadOrder.CreateLoadOrder(ModDefs, out var notLoaded, LoadOrder.FromFile(FilePaths.LoadOrderPath));
        foreach (var mod in notLoaded)
        {
            var reason = "Warning: Will not load " + mod.QuotedName;

            var conflicts = mod.CalcConflicts(ModLoadOrder);
            if (conflicts.Count > 0)
            {
                reason += "; conflicts: " + string.Join(", ", conflicts.Select(x => '"' + x + '"'));
            }

            var missingDependsOn = mod.CalcMissingDependsOn(ModLoadOrder);
            if (missingDependsOn.Count > 0)
            {
                reason += "; missing dependencies: " + string.Join(", ", missingDependsOn.Select(x => '"' + x + '"'));
            }
            OnModLoadFailure(mod.Name, reason);
        }
    }

    private static void OnModLoadFailure(string modName, string reason, Exception e = null, bool canIgnoreFailure = true)
    {
        if (e != null)
        {
            Log.Main.Warning?.Log(reason, e);
        }
        else
        {
            Log.Main.Warning?.Log(reason);
        }

        ModDefs.Remove(modName);

        if (allModDefs.TryGetValue(modName, out var modDef))
        {
            if (canIgnoreFailure && modDef.IgnoreLoadFailure)
            {
                return;
            }
            modDef.LoadFail = true;
            modDef.FailReason = reason;
        }

        FailedToLoadMods.Add(modName);
    }

    internal static void FinishedLoadingMods()
    {
        if (ModLoadOrder == null || ModLoadOrder.Count <= 0)
        {
            return;
        }

        {
            Log.Main.Info?.Log("Calling FinishedLoading:");
            foreach (var modDef in ModsInLoadOrder().Where(modDef => modDef.Assembly != null))
            {
                ModDefExLoading.FinishedLoading(modDef, ModLoadOrder);
            }
        }
        LoadOrder.ToFile(ModLoadOrder, FilePaths.LoadOrderPath);
    }
}