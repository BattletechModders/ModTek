using System;
using System.Collections.Generic;
using System.IO;
using BattleTech;
using ModTek.Features.AdvJSONMerge;
using ModTek.Misc;
using Newtonsoft.Json;
using static ModTek.Logging.Logger;
using MergeSets = System.Collections.Generic.Dictionary<string, ModTek.Features.Manifest.Merges.MergeCacheEntry>;

namespace ModTek.Features.Manifest.Merges
{
    internal class MergeCache
    {
        private static string PersistentFilePath => FilePaths.MergeCachePath;

        private readonly MergeSets persistentSets;
        private readonly MergeSets tempSets = new();

        internal MergeCache()
        {
            if (File.Exists(PersistentFilePath))
            {
                try
                {
                    persistentSets = JsonConvert.DeserializeObject<MergeSets>(File.ReadAllText(PersistentFilePath));
                    return;
                }
                catch (Exception e)
                {
                    LogException("Loading merge cache failed.", e);
                }
            }

            // create a new one if it doesn't exist or couldn't be added'
            Log("Building new Merge Cache.");
            persistentSets = new MergeSets();
        }

        internal void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(persistentSets, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(PersistentFilePath, json);
            }
            catch (Exception e)
            {
                Log($"Couldn't write merge cache to {PersistentFilePath}", e);
            }
        }

        internal bool HasMergedContentCached(VersionManifestEntry entry, bool fetchContent, out string cachedContent)
        {
            cachedContent = null;
            var key = CacheKeys.Unique(entry);
            if (!tempSets.TryGetValue(key, out var temp))
            {
                // lets find and fix un-typed sets
                // TODO this way a good idea? we ignore all untyped if we find one typed.. so no
                var noTypeKey = CacheKeys.Unique(entry);
                if (!tempSets.TryGetValue(noTypeKey, out temp))
                {
                    return false;
                }

                tempSets.Remove(noTypeKey);
                temp.ResourceType = entry.Type;
                tempSets[key] = temp;
            }

            temp.OriginalUpdatedOn = entry.UpdatedOn;

            if (!persistentSets.TryGetValue(key, out var persist))
            {
                return false;
            }

            if (!temp.Equals(persist))
            {
                return false;
            }

            try
            {
                if (fetchContent)
                {
                    cachedContent = File.ReadAllText(temp.CachedPath);
                }
                else if (!File.Exists(temp.CachedPath))
                {
                    return false;
                }
            }
            catch
            {
                Log($"Couldn't read cached merge result at {temp.CachedPath}");
                return false;
            }

            return true;
        }

        internal string MergeAndCacheContent(VersionManifestEntry entry, string originalContent)
        {
            if (originalContent == null)
            {
                return null;
            }

            var key = CacheKeys.Unique(entry);
            if (!tempSets.TryGetValue(key, out var temp))
            {
                return null;
            }

            string mergedContent;
            try
            {
                mergedContent = temp.Merge(originalContent);
            }
            catch (Exception e)
            {
                Log($"Couldn't merge {temp.CachedPath}", e);
                return null;
            }

            try
            {
                File.WriteAllText(temp.CachedPath, mergedContent);
                persistentSets[key] = temp;
            }
            catch (Exception e)
            {
                Log($"Couldn't write cached merge result to {temp.CachedPath}", e);
            }

            return mergedContent;
        }

        internal bool HasMerges(VersionManifestEntry entry)
        {
            var key = CacheKeys.Unique(entry);
            return tempSets.ContainsKey(key);
        }

        internal void AddModEntry(ModEntry entry)
        {
            if (entry.Type == BTConstants.CustomType_AdvancedJSONMerge)
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
                    Log($"\tError: AdvancedJSONMerge: \"{entry.RelativePathToMods}\" didn't target any IDs. Skipping merge.");
                    return;
                }

                foreach (var target in targets)
                {
                    var copy = entry.copy();
                    copy.Id = target;
                    copy.Type = advMerge.TargetType;
                    AddTemp(entry);
                }
            }
            else
            {
                AddTemp(entry);
            }
        }

        private void AddTemp(ModEntry entry)
        {
            var key = CacheKeys.Unique(entry);
            if (!tempSets.TryGetValue(key, out var set))
            {
                set = new MergeCacheEntry(entry);
                tempSets[key] = set;
            }

            set.Add(entry);
        }
    }
}
