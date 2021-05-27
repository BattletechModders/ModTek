using System;
using System.Collections.Generic;
using ModTek.Logging;
using ModTek.Manifest.AdvMerge;
using ModTek.Manifest.Merges.Cache;
using ModTek.Manifest.Mods;

namespace ModTek.Manifest.Merges
{
    internal class MergesDatabase
    {
        private const string DEFAULT_BUNDLE_NAME = "default"; // used for mod and streaming assets resources

        private readonly MergeCache mergeCache = new();

        // bundleName: the assetBundle file or StreamingAssets if null
        // id: the unique identifier, we use filename without extension since basegame+dlc doesnt have duplicates if json/txt/csv is involved
        // version: version of the base content
        // returns the cached version if available, null otherwise
        internal string GetMergedContent(string bundleName, string id, DateTime version)
        {
            return mergeCache.GetCachedContent(bundleName ?? DEFAULT_BUNDLE_NAME, id, version);
        }

        // returns the merged content, return null if nothing to merge or error happened
        internal string MergeContentIfApplicable(string bundleName, string id, DateTime version, string originalContent)
        {
            return mergeCache.MergeAndCacheContent(bundleName ?? DEFAULT_BUNDLE_NAME, id, version, originalContent);
        }

        internal void AddModEntry(ModEntry entry)
        {
            if (entry.Type == ModDefExLoading.CustomType_AdvancedJSONMerge)
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
            var bundleName = modEntry.AssetBundleName;

            mergeCache.AddTemp(bundleName, id, modEntry);
        }
    }
}
