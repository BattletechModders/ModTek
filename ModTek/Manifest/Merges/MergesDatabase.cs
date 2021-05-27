using System.Collections.Generic;
using BattleTech;
using ModTek.Logging;
using ModTek.Manifest.AdvMerge;
using ModTek.Manifest.Merges.Cache;
using ModTek.Manifest.Mods;

namespace ModTek.Manifest.Merges
{
    internal class MergesDatabase
    {
        private readonly MergeCache mergeCache = new();

        internal string GetMergedContent(VersionManifestEntry entry)
        {
            return mergeCache.GetCachedContent(entry);
        }

        internal string MergeContentIfApplicable(VersionManifestEntry entry, string originalContent)
        {
            return mergeCache.MergeAndCacheContent(entry, originalContent);
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
                    var copy = entry.copy();
                    copy.Id = target;
                    copy.Type = advMerge.TargetType;
                    mergeCache.AddTemp(entry);
                }
            }
            else
            {
                mergeCache.AddTemp(entry);
            }
        }
    }
}
