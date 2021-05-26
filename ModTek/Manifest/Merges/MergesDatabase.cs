using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTek.Logging;
using ModTek.Manifest.AdvMerge;
using ModTek.Misc;
using ModTek.Mods;
using ModTek.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModTek.Manifest.Merges
{
    internal class MergesDatabase
    {
        private readonly string FakeStreamingAssetsBundleName = "StreamingAssets";

        // asset bundle name OR StreamingAssets, id, list
        private readonly Dictionary<string, Dictionary<string, List<ModEntry>>> merges = new();
        private readonly MergeCache mergeResultsCache = new();

        internal void Clear()
        {
            merges.Clear();
            // mergeResultsCache.Clear();
        }

        internal void GetMergedStreamingAssets(string id, DateTime version, out string content, out bool hasMerges)
        {
            GetMergedAssetBundle(FakeStreamingAssetsBundleName, id, version, out content, out hasMerges);
        }

        // version: version of the base content
        // mergedContent: returns the cached version if available, null otherwise
        // canCache: returns true if there are merges available, null otherwise
        internal void GetMergedAssetBundle(string assetBundleName, string id, DateTime version, out string cachedMergedContent, out bool canMerge)
        {
            cachedMergedContent = null;
            canMerge = Entries(assetBundleName, id).Any();
        }

        // returns the merged content, can return already merged content
        internal void PutStreamingAssetsAndReturnMergedContent(string id, DateTime version, string originalContent, out string mergedContent)
        {
            PutAssetBundleAndReturnMergedContent(FakeStreamingAssetsBundleName, id, version, originalContent, out mergedContent);
        }

        // returns the merged content
        internal void PutAssetBundleAndReturnMergedContent(string assetBundleName, string id, DateTime version, string originalContent, out string mergedContent)
        {
            mergedContent = Merge(assetBundleName, id, originalContent);
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
            var bundleName = modEntry.AssetBundleName ?? FakeStreamingAssetsBundleName;

            if (!merges.TryGetValue(bundleName, out var ids))
            {
                ids = new Dictionary<string, List<ModEntry>>();
                merges[bundleName] = ids;
            }

            if (!ids.TryGetValue(id, out var list))
            {
                list = new List<ModEntry>();
                ids[id] = list;
            }

            list.Add(modEntry);
        }

        private List<ModEntry> Entries(string bundleName, string id)
        {
            if (merges.TryGetValue(bundleName, out var ids) && ids.TryGetValue(id, out var list))
            {
                return list;
            }
            return null;
        }

        private string Merge(string bundleName, string id, string originalContent)
        {
            var list = Entries(bundleName, id);
            if (list == null)
            {
                return null;
            }

            var target = JsonUtils.ParseGameJSON(originalContent);
            foreach (var entry in list)
            {
                var merge = JsonUtils.ParseGameJSON(entry.Path);
                JSONMerger.MergeIntoTarget(target, merge);
            }

            return target.ToString(Formatting.Indented);
        }

        // internal void Merge(JObject target)
        // {
        //     JSONMerger.MergeIntoTarget(target, JObjectCache.ParseGameJSONFile(FilePath));
        // }
        //
        // internal void Append(StreamWriter writer)
        // {
        //     writer.Write(File.ReadAllText(FilePath));
        // }
    }
}
