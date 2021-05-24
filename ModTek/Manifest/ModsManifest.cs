using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using Harmony;
using ModTek.Logging;
using ModTek.Manifest.AdvMerge;
using ModTek.Manifest.Merges;
using ModTek.Misc;
using ModTek.Mods;
using ModTek.SoundBanks;
using ModTek.UI;
using ModTek.Util;
using SVGImporter;

namespace ModTek.Manifest
{
    internal static class ModsManifest
    {
        private static MergesDatabase mergesDatabase = new();

        private static HashSet<string> systemIcons = new();
        internal static HashSet<ModEntry> CustomTags = new();
        internal static HashSet<ModEntry> CustomTagSets = new();

        internal static List<ModEntry> AddBTRLEntries = new();
        internal static List<VersionManifestEntry> RemoveBTRLEntries = new();
        internal static HashSet<string> BTRLEntriesPathes;

        internal static Dictionary<string, Dictionary<string, VersionManifestEntry>> CustomResources = new();
        internal static Dictionary<string, string> ModAssetBundlePaths { get; } = new();

        internal static void ClearTemp()
        {
            mergesDatabase.Clear();
        }

        internal static bool isInSystemIcons(string id)
        {
            return systemIcons.Contains(id);
        }

        internal static void AddModEntry(ModEntry modEntry)
        {
            if (modEntry.Path == null)
            {
                return;
            }

            // since we're adding a new entry here, we want to remove anything that would remove it again after the fact
            if (RemoveBTRLEntries.RemoveAll(entry => entry.Id == modEntry.Id && entry.Type == modEntry.Type) > 0)
            {
                Logger.Log((string) $"\t\t{modEntry.Id} ({modEntry.Type}) -- this entry replaced an entry that was slated to be removed. Removed the removal.");
            }

            if (CustomResources.ContainsKey(modEntry.Type))
            {
                Logger.Log((string) $"\tAdd/Replace (CustomResource): \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type})");
                CustomResources[modEntry.Type][modEntry.Id] = modEntry.GetVersionManifestEntry();
                return;
            }

            VersionManifestAddendum addendum = null;
            if (!string.IsNullOrEmpty(modEntry.AddToAddendum))
            {
                addendum = ModDefsDatabase.CachedVersionManifest.GetAddendumByName(modEntry.AddToAddendum);

                if (addendum == null)
                {
                    Logger.Log((string) $"\tWarning: Cannot add {modEntry.Id} to {modEntry.AddToAddendum} because addendum doesn't exist in the manifest.");
                    return;
                }
            }

            // special handling for particular types
            switch (modEntry.Type)
            {
                case "AssetBundle":
                    ModAssetBundlePaths[modEntry.Id] = modEntry.Path;
                    break;
                case nameof(SoundBankDef):
                    SoundBanksFeature.AddSoundBankDef(modEntry.Path);
                    return;
                case nameof(SVGAsset):
                    Logger.Log((string) $"Processing SVG entry of: {modEntry.Id}  type: {modEntry.Type}  name: {nameof(SVGAsset)}  path: {modEntry.Path}");
                    if (modEntry.Id.StartsWith(nameof(UILookAndColorConstants)))
                    {
                        systemIcons.Add(modEntry.Id);
                    }

                    break;
                case ModDefExLoading.CustomType_Tag:
                    CustomTags.Add(modEntry);
                    return; // Do not process further and do when the DB is updated
                case ModDefExLoading.CustomType_TagSet:
                    CustomTagSets.Add(modEntry);
                    return; // Do no process further and do when the DB is updated
            }

            // add to addendum instead of adding to manifest
            if (addendum != null)
            {
                Logger.Log((string) $"\tAdd/Replace: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type}) [{addendum.Name}]");
            }
            else
            {
                Logger.Log((string) $"\tAdd/Replace: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type})");
            }

            // entries in AddBTRLEntries will be added to game through patch in Patches\BattleTechResourceLocator
            AddBTRLEntries.Add(modEntry);
        }

        internal static bool RemoveEntry(string id, TypeCache typeCache)
        {
            var removedEntry = false;

            var containingCustomTypes = Enumerable.Where<KeyValuePair<string, Dictionary<string, VersionManifestEntry>>>(CustomResources, pair => pair.Value.ContainsKey(id)).ToList();
            foreach (var pair in containingCustomTypes)
            {
                Logger.Log((string) $"\tRemove: \"{pair.Value[id].Id}\" ({pair.Value[id].Type}) - Custom Resource");
                pair.Value.Remove(id);
                removedEntry = true;
            }

            var modEntries = AddBTRLEntries.FindAll(entry => entry.Id == id);
            foreach (var modEntry in modEntries)
            {
                Logger.Log((string) $"\tRemove: \"{modEntry.Id}\" ({modEntry.Type}) - Mod Entry");
                AddBTRLEntries.Remove(modEntry);
                BTRLEntriesPathes.Remove(modEntry.Path);
                removedEntry = true;
            }

            var vanillaEntries = ModDefsDatabase.CachedVersionManifest.FindAll(entry => entry.Id == id);
            foreach (var vanillaEntry in vanillaEntries)
            {
                Logger.Log((string) $"\tRemove: \"{vanillaEntry.Id}\" ({vanillaEntry.Type}) - Vanilla Entry");
                RemoveBTRLEntries.Add(vanillaEntry);
                removedEntry = true;
            }

            var types = typeCache.GetTypes(id, ModDefsDatabase.CachedVersionManifest);
            foreach (var type in types)
            {
                mergesDatabase.RemoveMerge(type, id);
            }

            return removedEntry;
        }

        internal static void PrepareManifestAndCustomResources()
        {
            ModDefsDatabase.CachedVersionManifest = VersionManifestUtilities.LoadDefaultManifest();

            // setup custom resources for ModTek types with fake VersionManifestEntries
            CustomResources.Add("Video", new Dictionary<string, VersionManifestEntry>());
            CustomResources.Add("SoundBank", new Dictionary<string, VersionManifestEntry>());

            // We intentionally DO NOT add tags and tagsets here, because AddModEntry() will skip values found in here
            //CustomResources[CustomType_Tag] = new Dictionary<string, VersionManifestEntry>();
            //CustomResources[CustomType_TagSet] = new Dictionary<string, VersionManifestEntry>();

            CustomResources.Add("DebugSettings", new Dictionary<string, VersionManifestEntry>());
            CustomResources["DebugSettings"]["settings"] = new VersionManifestEntry(
                "settings",
                Path.Combine(FilePaths.StreamingAssetsDirectory, FilePaths.DebugSettingsPath),
                "DebugSettings",
                DateTime.Now,
                "1"
            );

            CustomResources.Add("GameTip", new Dictionary<string, VersionManifestEntry>());
            CustomResources["GameTip"]["general"] = new VersionManifestEntry(
                "general",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "general.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
            CustomResources["GameTip"]["combat"] = new VersionManifestEntry(
                "combat",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "combat.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
            CustomResources["GameTip"]["lore"] = new VersionManifestEntry(
                "lore",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "lore.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
            CustomResources["GameTip"]["sim"] = new VersionManifestEntry(
                "sim",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "sim.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
        }

        internal static IEnumerator<ProgressReport> ProcessLoop()
        {
            return CSharpUtils.Enumerate(HandleModManifestsLoop(), mergesDatabase.MergeFilesLoop());
        }

        private static IEnumerator<ProgressReport> HandleModManifestsLoop()
        {
            // there are no mods loaded, just return
            if (ModDefsDatabase.ModLoadOrder == null || ModDefsDatabase.ModLoadOrder.Count == 0)
            {
                yield break;
            }

            Logger.Log((string) "\nAdding Mod Content...");
            var typeCache = new TypeCache(FilePaths.TypeCachePath);
            typeCache.UpdateToIDBased();
            Logger.Log((string) "");

            // progress panel setup
            var entryCount = 0;
            var numEntries = 0;
            ModDefsDatabase.ModDefs.Do(entries => numEntries += entries.Value.Manifest.Count);

            var manifestMods = ModDefsDatabase.ModLoadOrder.Where(name => ModDefsDatabase.ModDefs.ContainsKey(name) && (ModDefsDatabase.ModDefs[name].Manifest.Count > 0 || ModDefsDatabase.ModDefs[name].RemoveManifestEntries.Count > 0))
                .ToList();
            foreach (var modName in manifestMods)
            {
                var modDef = ModDefsDatabase.ModDefs[modName];

                Logger.Log((string) $"{modName}:");
                yield return new ProgressReport(entryCount / (float) numEntries, $"Loading {modName}", "", true);

                foreach (var modEntry in modDef.Manifest)
                {
                    yield return new ProgressReport(entryCount++ / (float) numEntries, $"Loading {modName}", modEntry.Id);

                    // type being null means we have to figure out the type from the path (StreamingAssets)
                    if (modEntry.Type == null)
                    {
                        var relativePath = FileUtils.GetRelativePath(modEntry.Path, Path.Combine(modDef.Directory, "StreamingAssets"));

                        if (relativePath == FilePaths.DebugSettingsPath)
                        {
                            modEntry.Type = "DebugSettings";
                        }
                    }

                    // type *still* being null means that this is an "non-special" case, i.e. it's in the manifest
                    if (modEntry.Type == null)
                    {
                        var relativePath = FileUtils.GetRelativePath(modEntry.Path, Path.Combine(modDef.Directory, "StreamingAssets"));
                        var fakeStreamingAssetsPath = Path.GetFullPath(Path.Combine(FilePaths.StreamingAssetsDirectory, relativePath));
                        if (!File.Exists(fakeStreamingAssetsPath))
                        {
                            Logger.Log((string) $"\tWarning: Could not find a file at {fakeStreamingAssetsPath} for {modName} {modEntry.Id}. NOT LOADING THIS FILE");
                            continue;
                        }

                        var types = typeCache.GetTypes(modEntry.Id, ModDefsDatabase.CachedVersionManifest);
                        if (types == null)
                        {
                            Logger.Log((string) $"\tWarning: Could not find an existing VersionManifest entry for {modEntry.Id}. Is this supposed to be a new entry? Don't put new entries in StreamingAssets!");
                            continue;
                        }

                        // TODO fix typeCache becoming irrelevant!
                        // this is getting merged later and then added to the BTRL entries then
                        // StreamingAssets don't get default appendText
                        if (Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".json" && modEntry.ShouldMergeJSON)
                        {
                            // this assumes that vanilla .json can only have a single type
                            // typeCache will always contain this path
                            modEntry.Type = typeCache.GetTypes(modEntry.Id)[0];
                            mergesDatabase.AddMerge(modEntry.Type, modEntry.Id, modEntry.Path);
                            Logger.Log((string) $"\tMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type})");
                            continue;
                        }

                        // TODO WTF IS THIS?? add stuff for every type? seems fishy for JSON stuff
                        // and why remove merge?
                        foreach (var type in types)
                        {
                            var subModEntry = new ModEntry(modEntry, modEntry.Path, modEntry.Id);
                            subModEntry.Type = type;
                            AddModEntry(subModEntry);
                            mergesDatabase.RemoveMerge(type, modEntry.Id);
                        }

                        continue;
                    }

                    // TODO WHY ARE TYPES SO IMPORTANT?????
                    // special handling for types
                    switch (modEntry.Type)
                    {
                        case ModDefExLoading.CustomType_AdvancedJSONMerge:
                        {
                            var advancedJSONMerge = AdvancedJSONMerge.FromFile(modEntry.Path);

                            if (!string.IsNullOrEmpty(advancedJSONMerge.TargetID) && advancedJSONMerge.TargetIDs == null)
                            {
                                advancedJSONMerge.TargetIDs = new List<string> { advancedJSONMerge.TargetID };
                            }

                            if (advancedJSONMerge.TargetIDs == null || advancedJSONMerge.TargetIDs.Count == 0)
                            {
                                Logger.Log((string) $"\tError: AdvancedJSONMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" didn't target any IDs. Skipping this merge.");
                                continue;
                            }

                            foreach (var id in advancedJSONMerge.TargetIDs)
                            {
                                var type = advancedJSONMerge.TargetType;
                                if (string.IsNullOrEmpty(type))
                                {
                                    var types = typeCache.GetTypes(id, ModDefsDatabase.CachedVersionManifest);
                                    if (types == null || types.Count == 0)
                                    {
                                        Logger.Log((string) $"\tError: AdvancedJSONMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" could not resolve type for ID: {id}. Skipping this merge");
                                        continue;
                                    }

                                    // assume that only a single type
                                    type = types[0];
                                }

                                var entry = FindEntry(type, id);
                                if (entry == null)
                                {
                                    Logger.Log((string) $"\tError: AdvancedJSONMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" could not find entry {id} ({type}). Skipping this merge");
                                    continue;
                                }

                                mergesDatabase.AddMerge(type, id, modEntry.Path);
                                Logger.Log((string) $"\tAdvancedJSONMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" targeting '{id}' ({type})");
                            }

                            continue;
                        }
                        case nameof(SoundBankDef):
                        {
                            AddModEntry(modEntry);
                            continue;
                        }
                        case ModDefExLoading.CustomType_FixedSVGAsset:
                        {
                            AddModEntry(modEntry);
                            continue;
                        }
                        case ModDefExLoading.CustomType_Tag:
                        {
                            Logger.Log((string) $"Processing tag of: {modEntry.Id} with type: {modEntry.Type} with path: {modEntry.Path}");
                            AddModEntry(modEntry);
                            continue;
                        }
                        case ModDefExLoading.CustomType_TagSet:
                        {
                            Logger.Log((string) $"Processing tagset of: {modEntry.Id} with type: {modEntry.Type} with path: {modEntry.Path}");
                            AddModEntry(modEntry);
                            continue;
                        }
                    }

                    // non-StreamingAssets json merges
                    if (Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".json" && modEntry.ShouldMergeJSON ||
                        (Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".txt" || Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".csv") && modEntry.ShouldAppendText)
                    {
                        // have to find the original path for the manifest entry that we're merging onto
                        var matchingEntry = FindEntry(modEntry.Type, modEntry.Id);

                        if (matchingEntry == null)
                        {
                            Logger.Log((string) $"\tWarning: Could not find an existing VersionManifest entry for {modEntry.Id}!");
                            continue;
                        }

                        // this assumes that .json can only have a single type
                        typeCache.TryAddType(modEntry.Id, modEntry.Type);
                        Logger.Log((string) $"\tMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type})");
                        mergesDatabase.AddMerge(modEntry.Type, modEntry.Id, modEntry.Path);
                        continue;
                    }

                    typeCache.TryAddType(modEntry.Id, modEntry.Type);
                    AddModEntry(modEntry);
                    mergesDatabase.RemoveMerge(modEntry.Type, modEntry.Id);
                }

                foreach (var removeID in ModDefsDatabase.ModDefs[modName].RemoveManifestEntries)
                {
                    if (!RemoveEntry(removeID, typeCache))
                    {
                        Logger.Log($"\tWarning: Could not find manifest entries for {removeID} to remove them. Skipping.");
                    }
                }
            }

            typeCache.ToFile(FilePaths.TypeCachePath);
            BTRLEntriesPathes = new HashSet<string>(AddBTRLEntries.Select(e => e.Path));
        }

        internal static VersionManifestEntry FindEntry(string type, string id)
        {
            if (CustomResources.ContainsKey(type) && CustomResources[type].ContainsKey(id))
            {
                return CustomResources[type][id];
            }

            var modEntry = AddBTRLEntries.FindLast(x => x.Type == type && x.Id == id)?.GetVersionManifestEntry();
            if (modEntry != null)
            {
                return modEntry;
            }

            // if we're slating to remove an entry, then we don't want to return it here from the manifest
            return !RemoveBTRLEntries.Exists(entry => entry.Type == type && entry.Id == id)
                ? ModDefsDatabase.CachedVersionManifest.Find(entry => entry.Type == type && entry.Id == id)
                : null;
        }

        internal static void FinalizeResourceLoading()
        {
            if (CustomResources["DebugSettings"]["settings"].FilePath != Path.Combine(FilePaths.StreamingAssetsDirectory, FilePaths.DebugSettingsPath))
            {
                DebugBridge.LoadSettings(CustomResources["DebugSettings"]["settings"].FilePath);
            }
        }
    }
}
