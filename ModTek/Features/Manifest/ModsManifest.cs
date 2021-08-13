using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.AdvJSONMerge;
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
        private static readonly MergeCache mergeCache = new();
        private static readonly MDDBCache mddbCache = new();

        internal static IEnumerator<ProgressReport> HandleModManifestsLoop()
        {
            var mods = ModDefsDatabase.ModsInLoadOrder();
            LogIf(mods.Count > 0, "Adding Mod Content...");

            foreach (var (modDef, index) in mods.WithIndex())
            {
                yield return new ProgressReport(index / (float) mods.Count, $"Loading {modDef.Name}", "", true);

                AddImplicitManifest(modDef);

                LogIf(modDef.Manifest.Count> 0, $"{modDef.Name} Manifest:");
                var packager = new ModAddendumPackager(modDef.Name);
                foreach (var modEntry in modDef.Manifest)
                {
                    NormalizeAndExpandAndAddModEntries(modDef, modEntry, packager);
                }
                packager.SaveToBTRL();

                LogIf(modDef.DataAddendumEntries.Count > 0, $"{modDef.Name} DataAddendum:");
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
            if (!modDef.LoadImplicitManifest)
            {
                return;
            }

            if (Directory.Exists(modDef.GetFullPath(FilePaths.StreamingAssetsDirectoryName)))
            {
                modDef.Manifest.Add(new ModEntry
                {
                    Path = FilePaths.StreamingAssetsDirectoryName,
                    ShouldMergeJSON = ModTek.Config.ImplicitManifestShouldMergeJSON,
                    ShouldAppendText = ModTek.Config.ImplicitManifestShouldAppendText
                });
            }

            if (Directory.Exists(modDef.GetFullPath(FilePaths.ModdedContentPackDirectoryName)))
            {
                modDef.Manifest.Add(new ModEntry
                {
                    Path = FilePaths.ModdedContentPackDirectoryName,
                    ShouldMergeJSON = ModTek.Config.ImplicitManifestShouldMergeJSON,
                    ShouldAppendText = ModTek.Config.ImplicitManifestShouldAppendText
                });
            }
        }

        private static void NormalizeAndExpandAndAddModEntries(ModDefEx modDef, ModEntry entry, ModAddendumPackager packager)
        {
            entry.ModDef = modDef;

            if (entry.AssetBundleName != null)
            {
                AddModEntry(entry, packager);
            }
            else if (entry.IsFile)
            {
                if (entry.Type == BTConstants.CustomType_AdvancedJSONMerge)
                {
                    ExpandAdvancedMerges(entry, packager);
                }
                else
                {
                    AddModEntry(entry, packager);
                }

            }
            else if (entry.IsDirectory)
            {
                if (entry.IsModdedContentPackBasePath)
                {
                    ExpandModdedContentPack(modDef, entry, packager);
                }
                else
                {
                    var patterns = entry.Type == nameof(SoundBankDef) ? new []{FileUtils.JSON_TYPE} : null;
                    foreach (var file in FileUtils.FindFiles(entry.AbsolutePath, patterns))
                    {
                        var copy = entry.copy();
                        copy.Path = FileUtils.GetRelativePath(modDef.Directory, file);
                        NormalizeAndExpandAndAddModEntries(modDef, copy, packager); // could lead to adv json merges that again expand
                    }
                }
            }
            else
            {
                Log($"\tWarning: Could not find path {entry.RelativePathToMods}.");
            }
        }

        private static void ExpandAdvancedMerges(ModEntry entry, ModAddendumPackager packager)
        {
            var advMerge = AdvancedJSONMerge.FromFile(entry.AbsolutePath);
            if (advMerge == null)
            {
                return;
            }

            var targets = new List<string>();
            if (!string.IsNullOrEmpty(advMerge.TargetID))
            {
                targets.Add(advMerge.TargetID);
            }

            if (advMerge.TargetIDs != null)
            {
                targets.AddRange(advMerge.TargetIDs);
            }

            if (targets.Count == 0)
            {
                targets.Add(entry.FileNameWithoutExtension);
            }

            foreach (var target in targets)
            {
                var copy = entry.copy();
                copy.Id = target;
                copy.Type = advMerge.TargetType;
                copy.ShouldMergeJSON = true;
                AddModEntry(copy, packager);
            }
        }

        private static void ExpandModdedContentPack(ModDefEx modDef, ModEntry entry, ModAddendumPackager packager)
        {
            foreach (var packPath in Directory.GetDirectories(entry.AbsolutePath))
            {
                var contentPackName = Path.GetFileName(packPath);
                if (!BTConstants.HBSContentNames.Contains(contentPackName))
                {
                    Log($"Unknown content pack {contentPackName} in {entry.AbsolutePath}");
                }

                foreach (var typesPath in Directory.GetDirectories(packPath))
                {
                    var typeName = Path.GetFileName(typesPath);
                    if (!BTConstants.ResourceType(typeName, out _))
                    {
                        Log($"Unknown resource type {typeName} in {packPath}");
                        continue;
                    }

                    foreach (var file in FileUtils.FindFiles(typesPath))
                    {
                        var copy = entry.copy();
                        copy.Path = FileUtils.GetRelativePath(modDef.Directory, file);
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
            if (!FixMissingIdAndType(entry))
            {
                return;
            }

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

            Log($"\tError: Type of entry unknown: {entry}.");
        }

        private static bool FixMissingIdAndType(ModEntry entry)
        {
            // fix missing id
            if (string.IsNullOrEmpty(entry.Id))
            {
                entry.Id = entry.FileNameWithoutExtension;
            }

            if (!string.IsNullOrEmpty(entry.Type))
            {
                return true;
            }

            if (CustomStreamingAssetsFeature.FindAndSetMatchingCustomStreamingAssetsType(entry))
            {
                return true;
            }

            var ext = entry.Path.GetExtension();
            var entriesById = BetterBTRL.Instance.EntriesByID(entry.Id)
                .Where(x => ext.Equals(x.GetRawPath().GetExtension()))
                .ToList();

            switch (entriesById.Count)
            {
                case 0:
                    Log($"\t\tError: Can't resolve type, no types found for id and extension, please specify manually.");
                    return false;
                case > 1:
                    Log($"\t\tError: Can't resolve type, more than one type found for id and extension, please specify manually.");
                    return false;
                default:
                    entry.Type = entriesById[0].Type;
                    return true;
            }
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
                PreloadResourcesAfterManifestComplete(rebuildMDDB, preloadResources); //flagForRebuild, requestLoad
            }
            else
            {
                Log("Skipping preload, no changes in MDDB data detected.");
                SaveCaches();
            }
        }

        private static readonly Stopwatch preloadSW = new();
        private static void PreloadResourcesAfterManifestComplete(bool rebuildMDDB, HashSet<CacheKey> preloadResources)
        {
            if (!ModTek.Config.PreloadResourcesForCache)
            {
                Log("Skipped preloading resources");
                SaveCaches();
                return;
            }
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
                    mddbCache.Add(entry, content, false);
                }
            }
            else
            {
                mddbCache.Add(entry, content, true);
            }
        }
    }
}
