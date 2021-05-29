using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.CustomSVGAssets;
using ModTek.Features.CustomTypes;
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
                return;
            }

            if (entry.IsTypeBattleTechResourceType)
            {
                var resourceType = entry.ResourceType;
                if (resourceType is BattleTechResourceType.SVGAsset)
                {
                    SVGAssetFeature.OnAddSVGEntry(entry);
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

                return;
            }

            if (CustomResourcesFeature.Add(entry))
            {
                return;
            }

            if (!SoundBanksFeature.Add(entry))
            {
                return;
            }

            if (CustomTagFeature.Add(entry))
            {
                return;
            }

            Log($"\tError: Type of entry unknown: \"{entry.RelativePathToMods}\".");
        }

        internal static void BTRLContentPackLoaded()
        {
            PreloadAfterManifestComplete();
        }

        private static Stopwatch osw = new();
        private static void PreloadAfterManifestComplete()
        {
            osw.Restart();

            // how to detect deletion? -> only possible after index loaded

            // default + mods-non-dlc | index + merge -> almost immediately
            // check for changes here not possible since it might be overwritten by dlc + mods-dlc later

            // dlc + mods-dlc | index -> after index loaded

            // dlc + mods-dlc | merge -> after content loaded

            // TODO merge and dbcache stuff whats not DLC

            // TODO then do the same later

            // TODO don't do preload if we dont need to
            // if (manifest changed || merges_changed since last time)
            {
                var loadRequest = UnityGameInstance.BattleTechGame.DataManager.CreateLoadRequest(_ => SaveCaches());
                foreach (var type in BTConstants.MDDTypes)
                {
                    loadRequest.AddAllOfTypeBlindLoadRequest(type); // force build everything MDD related
                }
                loadRequest.ProcessRequests();
            }
        }

        private static void SaveCaches()
        {
            var sw = new Stopwatch();

            sw.Start();
            mergeCache.Save();
            sw.Stop();
            Log($"mergeCache.Save {sw.Elapsed.TotalSeconds}");

            sw.Start();
            mddbCache.Save();
            sw.Stop();
            Log($"mddbCache.Save {sw.Elapsed.TotalSeconds}");

            osw.Stop();
            Log($"PreloadAfterManifestComplete {osw.Elapsed.TotalSeconds}");
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
