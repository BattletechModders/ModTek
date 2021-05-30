using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.CustomSVGAssets;
using ModTek.Features.CustomTags;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;
using ModTek.Features.Manifest.Merges;
using ModTek.Features.Manifest.Mods;
using ModTek.Features.SoundBanks;
using ModTek.Misc;
using ModTek.UI;
using ModTek.Util;
using static ModTek.Logging.Logger;

namespace ModTek.Features.Manifest
{
    internal static class ModsManifest
    {
        private static MergeCache mergeCache = new();
        private static MDDBCache mddbCache = new();

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

            CustomTagFeature.ProcessTags();

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

                if (Directory.Exists(modDef.GetFullPath(FilePaths.AssetBundleMergesDirectoryName)))
                {
                    modDef.Manifest.Add(new ModEntry { Type = FilePaths.AssetBundleMergesDirectoryName, Path = FilePaths.AssetBundleMergesDirectoryName, ShouldMergeJSON = true, ShouldAppendText = true });
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

                if (entry.IsAssetBundleMergesBasePath)
                {
                    foreach (var bundlePath in Directory.GetDirectories(entry.AbsolutePath))
                    {
                        var bundleName = Path.GetFileName(bundlePath);
                        foreach (var file in FileUtils.FindFiles(bundlePath, ".json", ".csv", ".txt"))
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
                    var patterns = entry.Type == nameof(SoundBankDef) ? new []{".json"} : null;
                    foreach (var file in FileUtils.FindFiles(entry.AbsolutePath, patterns))
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
            var logType = string.IsNullOrEmpty(entry.Type) ? "" : $" ({entry.Type})";
            var logId = $"{entry.Id}{logType}: {entry.RelativePathToMods}";

            if (mergeCache.AddModEntry(entry))
            {
                Log($"\tMerge: {logId}");
                return;
            }

            if (entry.IsTypeBattleTechResourceType)
            {
                var resourceType = entry.ResourceType;
                if (resourceType is BattleTechResourceType.SVGAsset)
                {

                    Log($"\tSVGAsset: {logId}");
                    SVGAssetFeature.OnAddSVGEntry(entry);
                }

                if (!entry.AddToDB)
                {
                    Log($"\tAddToDB=false: {logId}");
                    mddbCache.Ignore(entry);
                }

                if (entry.AddToAddendum != null)
                {
                    Log($"\tAddToAddendum: {logId}");
                    BetterBTRL.Instance.AddAddendumOverrideEntry(entry.AddToAddendum, entry.CreateVersionManifestEntry());
                }
                else
                {
                    Log($"\tAdd/Replace: {logId}");
                    packager.AddEntry(entry);
                }

                return;
            }

            if (CustomResourcesFeature.Add(entry))
            {
                Log($"\tAdd/Replace (CustomResource): {logId}");
                return;
            }

            if (SoundBanksFeature.Add(entry))
            {
                Log($"\tAdd/Replace: {logId}");
                return;
            }

            if (CustomTagFeature.Add(entry))
            {
                Log($"\tAdd/Replace: {logId}");
                return;
            }

            Log($"\tError: Type of entry unknown: \"{entry.RelativePathToMods}\".");
        }

        internal static void VerifyCaches()
        {
            var requestLoad = new List<CacheKey>();

            var flagForRebuild = false;
            mergeCache.CleanCache(ref flagForRebuild, requestLoad);
            mddbCache.CleanCache(ref flagForRebuild, requestLoad);

            if (flagForRebuild || requestLoad.Count > 0)
            {
                PreloadMergesAfterManifestComplete(); //flagForRebuild, requestLoad
            }
            else
            {
                Log("Skipping preload, no changes in MDDB data detected.");
                SaveCaches();
            }
        }

        private static Stopwatch preloadSW = new();
        private static void PreloadMergesAfterManifestComplete()
        {
            preloadSW.Start();

            // TODO not even sure if we need this phase, but vanilla has all data preloaded in their MDDB
            Log("Preloading MDDB related data.");

            var loadRequest = UnityGameInstance.BattleTechGame.DataManager.CreateLoadRequest(_ => PreloadFinished());
            foreach (var type in BTConstants.CCTypes.Concat(BTConstants.MDDTypes.Except(BTConstants.CCTypes)))
            {
                loadRequest.AddAllOfTypeBlindLoadRequest(type); // force build everything MDD related
            }
            loadRequest.ProcessRequests();
        }

        private static void PreloadFinished()
        {
            preloadSW.Stop();
            LogIfSlow(preloadSW, "Preloading");
            SaveCaches();
        }

        internal static void SimGameOrSkirmishLoaded()
        {
            Log("Skirmish or SimGame loaded");
            SaveCaches();
        }

        private static void SaveCaches()
        {
            mergeCache.Save();
            mddbCache.Save();
        }

        internal static string GetMergedContent(VersionManifestEntry entry)
        {
            return mergeCache.HasMergedContentCached(entry, true, out var content) ? content : null;
        }

        internal static void ContentLoaded(VersionManifestEntry entry, ref string content)
        {
            if (mergeCache.HasMerges(entry))
            {
                if (!mergeCache.HasMergedContentCached(entry, false, out _))
                {
                    mergeCache.MergeAndCacheContent(entry, ref content);
                    mddbCache.Add(entry, content);
                }
            }
            else
            {
                mddbCache.Add(entry, content, true);
            }
        }
    }
}
