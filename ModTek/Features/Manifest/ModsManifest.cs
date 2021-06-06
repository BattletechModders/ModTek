using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.CustomStreamingAssets;
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
using static ModTek.Features.Logging.MTLogger;

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

                yield return new ProgressReport(entryCount++ / (float) numEntries, $"Loading {modName}", "", true);

                AddImplicitManifest(modDef);

                LogIf(modDef.Manifest.Count> 0, $"{modName} Manifest:");

                var packager = new ModAddendumPackager(modName);
                foreach (var modEntry in modDef.Manifest)
                {
                    NormalizeAndExpandAndAddModEntries(modDef, modEntry, packager);
                }

                packager.SaveToBTRL();

                LogIf(modDef.DataAddendumEntries.Count > 0, $"{modName}DataAddendum:");
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

                if (Directory.Exists(modDef.GetFullPath(FilePaths.ContentPackMergesDirectoryName)))
                {
                    modDef.Manifest.Add(new ModEntry { Path = FilePaths.ContentPackMergesDirectoryName, ShouldMergeJSON = true, ShouldAppendText = true });
                }
            }
        }

        private static void NormalizeAndExpandAndAddModEntries(ModDefEx modDef, ModEntry entry, ModAddendumPackager packager)
        {
            entry.ModDef = modDef;

            if (entry.IsFile)
            {
                if (string.IsNullOrEmpty(entry.Id))
                {
                    entry.Id = entry.FileNameWithoutExtension;
                }

                AddModEntry(entry, packager);
            }
            else if (entry.IsDirectory)
            {
                if (entry.IsContentPackMergesBasePath)
                {
                    ExpandContentPackMerges(modDef, entry, packager);
                }
                else
                {
                    var patterns = entry.Type == nameof(SoundBankDef) ? new []{"*"+FileUtils.JSON_TYPE} : null;
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

        private static void ExpandContentPackMerges(ModDefEx modDef, ModEntry entry, ModAddendumPackager packager)
        {
            foreach (var contentPackMergesPath in Directory.GetDirectories(entry.AbsolutePath))
            {
                var contentPackName = Path.GetFileName(contentPackMergesPath);
                if (!BTConstants.HBSContentNames.Contains(contentPackName))
                {
                    Log($"Unknown content pack {contentPackName} in {entry.AbsolutePath}");
                }

                foreach (var typesPath in Directory.GetDirectories(contentPackMergesPath))
                {
                    var typeName = Path.GetFileName(typesPath);
                    if (!BTConstants.ResourceType(typeName, out _))
                    {
                        Log($"Unknown resource type {typeName} in {contentPackMergesPath}");
                        continue;
                    }

                    foreach (var file in FileUtils.FindFiles(typesPath, FileUtils.JSON_TYPE, FileUtils.CSV_TYPE, FileUtils.TXT_TYPE))
                    {
                        var copy = entry.copy();
                        copy.Path = FileUtils.GetRelativePath(modDef.Directory, file);
                        copy.Id = Path.GetFileNameWithoutExtension(file);
                        copy.Type = typeName;
                        copy.RequiredContentPacks = new[]
                        {
                            contentPackName
                        };
                        AddModEntry(copy, packager);
                    }
                }
            }
        }

        private static void AddModEntry(ModEntry entry, ModAddendumPackager packager)
        {
            CustomStreamingAssetsFeature.FindAndSetMatchingCustomStreamingAssetsType(entry);

            if (mergeCache.AddModEntry(entry))
            {
                return;
            }

            if (entry.IsTypeBattleTechResourceType)
            {
                var resourceType = entry.ResourceType;
                if (resourceType is BattleTechResourceType.SVGAsset)
                {

                    Log($"\tSVGAsset: {entry}");
                    SVGAssetFeature.OnAddSVGEntry(entry);
                }

                if (!entry.AddToDB)
                {
                    Log($"\tAddToDB=false: {entry}");
                    mddbCache.Ignore(entry);
                }

                if (entry.AddToAddendum != null)
                {
                    Log($"\tAddToAddendum: {entry}");
                    BetterBTRL.Instance.AddAddendumOverrideEntry(entry.AddToAddendum, entry.CreateVersionManifestEntry());
                }
                else
                {
                    Log($"\tAdd/Replace: {entry}");
                    packager.AddEntry(entry);
                }
                return;
            }

            if (entry.IsTypeCustomStreamingAsset)
            {
                packager.AddEntry(entry);
                return;
            }

            if (entry.IsTypeCustomResource)
            {
                Log($"\tAdd/Replace: {entry}");
                if (entry.RequiredContentPacks != null && entry.RequiredContentPacks.Length > 0)
                {
                    Log($"\t\tError: Custom resources don't support RequiredContentPacks.");
                    return;
                }
                packager.AddEntry(entry);
                return;
            }

            if (SoundBanksFeature.Add(entry))
            {
                Log($"\tAdd/Replace: {entry}");
                return;
            }

            if (CustomTagFeature.Add(entry))
            {
                Log($"\tAdd/Replace: {entry}");
                return;
            }

            Log($"\tError: Type of entry unknown: \"{entry.RelativePathToMods}\".");
        }

        internal static void VerifyCaches()
        {
            Log();
            var preloadResources = new HashSet<CacheKey>();

            var rebuildMDDB = false;
            var sw = new Stopwatch();
            sw.Start();
            mergeCache.CleanCacheWithCompleteManifest(ref rebuildMDDB, preloadResources);
            sw.Stop();
            LogIfSlow(sw, "Merge Cache Cleanup");
            sw.Restart();
            mddbCache.CleanCacheWithCompleteManifest(ref rebuildMDDB, preloadResources);
            sw.Stop();
            LogIfSlow(sw, "MDDB Cache Cleanup");

            if (rebuildMDDB || preloadResources.Count > 0)
            {
                PreloadMergesAfterManifestComplete(rebuildMDDB, preloadResources); //flagForRebuild, requestLoad
            }
            else
            {
                Log("Skipping preload, no changes in MDDB data detected.");
                SaveCaches();
            }
        }

        private static Stopwatch preloadSW = new();
        private static void PreloadMergesAfterManifestComplete(bool rebuildMDDB, HashSet<CacheKey> preloadResources)
        {
            preloadSW.Start();

            // TODO not even sure if we need this phase, but vanilla has all data preloaded in their MDDB
            Log("Preloading MDDB related data.");

            var loadRequest = UnityGameInstance.BattleTechGame.DataManager.CreateLoadRequest(_ => PreloadFinished());
            if (rebuildMDDB)
            {
                foreach (var type in BTConstants.MDDBTypes)
                {
                    loadRequest.AddAllOfTypeBlindLoadRequest(type); // force build everything MDD related
                }
            }
            else
            {
                foreach (var resource in preloadResources)
                {
                    if (BTConstants.ResourceType(resource.Type, out var resourceType))
                    {
                        loadRequest.AddBlindLoadRequest(resourceType, resource.Id);
                    }
                }
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

        internal static string GetMergedContentOrReadAllTextAndMerge(VersionManifestEntry entry)
        {
            var content = GetMergedContent(entry);
            if (content == null)
            {
                content = File.ReadAllText(entry.FilePath);
                MergeContentIfApplicable(entry, ref content);
            }
            return content;
        }

        internal static string GetMergedContent(VersionManifestEntry entry)
        {
            return mergeCache.HasMergedContentCached(entry, true, out var content) ? content : null;
        }

        internal static void MergeContentIfApplicable(VersionManifestEntry entry, ref string content)
        {
            if (mergeCache.HasMerges(entry))
            {
                if (!mergeCache.HasMergedContentCached(entry, false, out _))
                {
                    mergeCache.MergeAndCacheContent(entry, ref content);
                    // merges dont modify the UpdateOn timestamp, force update MDDB here!
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
