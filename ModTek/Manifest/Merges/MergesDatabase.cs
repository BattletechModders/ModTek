using System;
using System.Collections.Generic;
using System.IO;
using ModTek.Logging;
using ModTek.Manifest.AdvMerge;
using ModTek.Manifest.Merges.Cache;
using ModTek.Mods;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Manifest.Merges
{
    internal class MergesDatabase
    {
        internal const string STREAMING_ASSETS_BUNDLE_NAME = "StreamingAssets";

        private readonly MergeCache mergeCache = new();

        // bundleName: the assetBundle file or StreamingAssets, see constant
        // id: the unique identifier, we use filename without extension since basegame+dlc doesnt have duplicates if json/txt/csv is involved
        // version: version of the base content
        // mergedContent: returns the cached version if available, null otherwise
        // canCache: returns true if there are merges available, null otherwise
        internal void GetMerged(string bundleName, string id, DateTime version, out string mergedContent, out bool canMerge)
        {
            canMerge = mergeCache.GetTemp(bundleName, id) != null;
            if (!canMerge)
            {
                mergedContent = null;
                return;
            }
            mergedContent = mergeCache.GetCachedContent(bundleName, id, version);
        }

        // returns the merged content
        internal void Merge(string bundleName, string id, DateTime version, string originalContent, out string mergedContent)
        {
            mergedContent = mergeCache.MergeAndCacheContent(bundleName, id, version, originalContent);
        }

        internal void AddModEntry(ModEntry entry)
        {
            if (entry.Type == ModDefExLoading.CustomType_AdvancedJSONMerge)
            {
                var advMerge = AdvancedJSONMerge.FromFile(entry.Path);
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
                    Logger.Log($"\tError: AdvancedJSONMerge: \"{entry.RelativePathToMods}\" didn't target any IDs. Skipping merge.");
                    return;
                }

                foreach (var target in targets)
                {
                    AddMergeEntry(entry, target);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(entry.Type))
                {
                    Logger.Log($"\tWarning: Type is ignore for merge \"{entry.RelativePathToMods}\".");
                }
                AddMergeEntry(entry);
            }
        }

        private void AddMergeEntry(ModEntry modEntry, string id = null)
        {
            id ??= modEntry.Id;
            var bundleName = modEntry.AssetBundleName ?? STREAMING_ASSETS_BUNDLE_NAME;

            mergeCache.AddTemp(bundleName, id, modEntry);
        }
    }
}
