using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BattleTech;
using BattleTech.UI;
using ModTek.Manifest.BTRL;
using ModTek.Manifest.MDD;
using ModTek.Manifest.MDD.CustomTypes;
using ModTek.Manifest.Merges;
using ModTek.Manifest.Mods;
using ModTek.Misc;
using ModTek.SoundBanks;
using ModTek.UI;
using ModTek.Util;
using SVGImporter;
using static ModTek.Logging.Logger;

namespace ModTek.Manifest
{
    internal static class ModsManifest
    {
        private static MergeCache mergeCache = new();
        private static MDDBCache mddbCache = new();

        private static HashSet<string> systemIcons = new();
        private static HashSet<ModEntry> CustomTags = new();
        private static HashSet<ModEntry> CustomTagSets = new();

        internal static Dictionary<string, Dictionary<string, VersionManifestEntry>> CustomResources = new();

        internal static bool isInSystemIcons(string id)
        {
            return systemIcons.Contains(id);
        }

        internal static void PrepareManifestAndCustomResources()
        {
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

        internal static IEnumerator<ProgressReport> HandleModManifestsLoop()
        {
            // there are no mods loaded, just return
            if (ModDefsDatabase.ModLoadOrder == null || ModDefsDatabase.ModLoadOrder.Count == 0)
            {
                yield break;
            }

            Log("\nAdding Mod Content...");

            // progress panel setup
            var entryCount = 0;
            var mods = ModDefsDatabase.ModsInLoadOrder();
            var numEntries = mods.Count;

            foreach (var modDef in mods)
            {
                var modName = modDef.Name;

                Log($"{modName}:");
                yield return new ProgressReport(entryCount++ / (float) numEntries, $"Loading {modName}", "", true);

                AddImplicitManifest(modDef);

                var packager = new ModAddendumPackager(modName);
                foreach (var modEntry in modDef.Manifest)
                {
                    NormalizeAndAddModEntries(modDef, modEntry, packager);
                }

                packager.SaveToBTRL();

                LogIf(modDef.DataAddendumEntries.Count > 0, "DataAddendum:");
                foreach (var dataAddendumEntry in modDef.DataAddendumEntries)
                {
                    if (AddendumUtils.LoadDataAddendum(dataAddendumEntry, modDef.Directory))
                    {
                        MDDBCache.HasChanges = true;
                    }
                }
            }

            LogIf(CustomTags.Count > 0, "Processing CustomTags:");
            foreach (var modEntry in CustomTags)
            {
                CustomTypeProcessor.AddOrUpdateTag(modEntry.AbsolutePath);
            }

            LogIf(CustomTagSets.Count > 0, "Processing CustomTagSets:");
            foreach (var modEntry in CustomTagSets)
            {
                CustomTypeProcessor.AddOrUpdateTagSet(modEntry.AbsolutePath);
            }

            BetterBTRL.Instance.RefreshTypedEntries();
        }

        private static void AddImplicitManifest(ModDefEx modDef)
        {
            if (modDef.LoadImplicitManifest)
            {
                if (Directory.Exists(modDef.GetFullPath(FilePaths.StreamingAssetsDirectoryName)))
                {
                    modDef.Manifest.Add(new ModEntry { Path = FilePaths.StreamingAssetsDirectoryName, ShouldMergeJSON = true, ShouldAppendText = true });
                }

                if (Directory.Exists(modDef.GetFullPath(FilePaths.AssetBundleDirectoryName)))
                {
                    modDef.Manifest.Add(new ModEntry { Type = FilePaths.AssetBundleDirectoryName, Path = FilePaths.AssetBundleDirectoryName, ShouldMergeJSON = true, ShouldAppendText = true });
                }
            }
        }

        private static void NormalizeAndAddModEntries(ModDefEx modDef, ModEntry entry, ModAddendumPackager packager)
        {
            entry.ModDef = modDef;

            if (entry.IsFile)
            {
                if (string.IsNullOrEmpty(entry.Id))
                {
                    entry.Id = entry.FileNameWithoutExtension;
                }

                if (entry.IsTypeStreamingAsset)
                {
                    if (entry.Id == "settings")
                    {
                        entry.Type = "DebugSettings";
                    }
                }

                AddModEntry(entry, packager);
            }
            else if (entry.IsDirectory)
            {
                if (entry.IsAssetBundlePath)
                {
                    foreach (var bundlePath in Directory.GetDirectories(entry.AbsolutePath))
                    {
                        var bundleName = Path.GetFileName(bundlePath);
                        foreach (var file in FileUtils.FindFiles(bundlePath, "*"))
                        {
                            var copy = entry.copy();
                            copy.AssetBundleName = bundleName; // TODO needed?
                            copy.Path = FileUtils.GetRelativePath(modDef.Directory, file);
                            copy.Id = Path.GetFileNameWithoutExtension(file);
                            AddModEntry(copy, packager);
                        }
                    }
                }
                else
                {
                    var pattern = entry.Type == nameof(SoundBankDef) ? "*.json" : "*";
                    foreach (var file in FileUtils.FindFiles(entry.AbsolutePath, pattern))
                    {
                        var copy = entry.copy();
                        copy.Path = FileUtils.GetRelativePath(modDef.Directory, file);
                        copy.Id = Path.GetFileNameWithoutExtension(file);
                        AddModEntry(copy, packager);
                    }
                }
            }
            else
            {
                Log($"\tWarning: Could not find path {entry.RelativePathToMods}.");
            }
        }

        private static void AddModEntry(ModEntry entry, ModAddendumPackager packager)
        {
            if (entry.ShouldMergeJSON || entry.ShouldAppendText)
            {
                if (entry.ShouldMergeJSON && entry.IsJson || entry.ShouldAppendText && (entry.IsTxt || entry.IsCsv))
                {
                    mergeCache.AddModEntry(entry);
                }
                else
                {
                    Log($"\tError: ShouldMergeJSON requires .json and ShouldAppendText requires .txt or .csv: \"{entry.RelativePathToMods}\".");
                }
            }
            else if (entry.IsTypeCustomResource)
            {
                Log($"\tAdd/Replace (CustomResource): \"{entry.RelativePathToMods}\" ({entry.Type})");
                CustomResources[entry.Type][entry.Id] = entry.CreateVersionManifestEntry();
            }
            else if (entry.IsTypeSoundBankDef)
            {
                SoundBanksFeature.AddSoundBankDef(entry.AbsolutePath);
            }
            else if (entry.IsTypeCustomTag)
            {
                CustomTags.Add(entry);
            }
            else if (entry.IsTypeCustomTagSet)
            {
                CustomTagSets.Add(entry);
            }
            else if (entry.IsTypeBattleTechResourceType)
            {
                var resourceType = entry.ResourceType;
                if (resourceType is BattleTechResourceType.SVGAsset)
                {
                    Log($"Processing SVG entry of: {entry.Id}  type: {entry.Type}  name: {nameof(SVGAsset)}  path: {entry.RelativePathToMods}");
                    if (entry.Id.StartsWith(nameof(UILookAndColorConstants)))
                    {
                        systemIcons.Add(entry.Id);
                    }
                }

                Log($"\tAdd/Replace: \"{entry.RelativePathToMods}\" ({entry.Type})");
                if (!entry.AddToDB)
                {
                    mddbCache.Ignore(entry);
                }

                if (entry.AddToAddendum != null)
                {
                    BetterBTRL.Instance.AddAddendumOverrideEntry(entry.AddToAddendum, entry.CreateVersionManifestEntry());
                }
                else
                {
                    packager.AddEntry(entry);
                }
            }
            else
            {
                Log($"\tError: Type of entry unknown: \"{entry.RelativePathToMods}\".");
            }
        }

        internal static void FinalizeResourceLoading()
        {
            if (CustomResources["DebugSettings"]["settings"].FilePath != Path.Combine(FilePaths.StreamingAssetsDirectory, FilePaths.DebugSettingsPath))
            {
                DebugBridge.LoadSettings(CustomResources["DebugSettings"]["settings"].FilePath);
            }
        }


        internal static void BTRLContentPackLoaded()
        {
            PreloadAfterManifestComplete();
        }

        private static Stopwatch osw = new();
        private static void PreloadAfterManifestComplete()
        {
            osw.Restart();

            var loadRequest = UnityGameInstance.BattleTechGame.DataManager.CreateLoadRequest(_ => SaveCaches());
            foreach (var type in BTResourceUtils.MDDTypes)
            {
                loadRequest.AddAllOfTypeBlindLoadRequest(type);
            }
            loadRequest.ProcessRequests();
        }

        private static void SaveCaches()
        {
            var sw = new Stopwatch();

            sw.Start();
            mergeCache.Save();
            sw.Stop();
            Log($"mergeCache.Save {sw.Elapsed}");

            sw.Start();
            mddbCache.Save();
            sw.Stop();
            Log($"mddbCache.Save {sw.Elapsed}");

            osw.Stop();
            Log($"PreloadAfterManifestComplete {osw.Elapsed}");
        }

        // only merges will be cached
        internal static string GetMergedContent(VersionManifestEntry entry)
        {
            return mergeCache.HasMergedContentCached(entry, true, out var content) ? content : null;
        }

        // make sure to update MDD if need be
        internal static string ContentLoaded(VersionManifestEntry entry, string content)
        {
            if (mergeCache.HasMerges(entry))
            {
                if (!mergeCache.HasMergedContentCached(entry, false, out _))
                {
                    content = mergeCache.MergeAndCacheContent(entry, content);
                    mddbCache.Add(entry, content, false);
                }
            }
            else
            {
                mddbCache.Add(entry, content);
            }

            return content;
        }
    }
}
