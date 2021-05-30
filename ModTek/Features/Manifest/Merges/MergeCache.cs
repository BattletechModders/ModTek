using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.AdvJSONMerge;
using ModTek.Misc;
using ModTek.Util;
using static ModTek.Logging.Logger;
using CacheDB = System.Collections.Generic.Dictionary<ModTek.Features.Manifest.CacheKey, ModTek.Features.Manifest.Merges.MergeCacheEntry>;
using CacheKeyValue = System.Collections.Generic.KeyValuePair<ModTek.Features.Manifest.CacheKey, ModTek.Features.Manifest.Merges.MergeCacheEntry>;

namespace ModTek.Features.Manifest.Merges
{
    internal class MergeCache
    {
        private static string PersistentDirPath => FilePaths.MergeCacheDirectory;
        private readonly string PersistentFilePath;

        private readonly CacheDB persistentSets; // stuff in here was merged
        private readonly CacheDB tempSets = new(); // stuff in here has merges queued
        private static bool HasChanges;

        internal MergeCache()
        {
            PersistentFilePath = Path.Combine(PersistentDirPath, "merge_cache.json");

            if (ModTekCacheStorage.CompressedExists(PersistentFilePath))
            {
                try
                {
                    persistentSets = ModTekCacheStorage.CompressedReadFrom<List<CacheKeyValue>>(PersistentFilePath)
                        .ToDictionary(kv => kv.Key, kv => kv.Value);;
                    return;
                }
                catch (Exception e)
                {
                    Log("Merge Cache: Loading merge cache failed.", e);
                }
            }

            FileUtils.CleanDirectory(PersistentDirPath);

            // create a new one if it doesn't exist or couldn't be added'
            Log("Merge Cache: Rebuilding cache.");
            persistentSets = new CacheDB();
        }

        private readonly Stopwatch saveSW = new();
        internal void Save()
        {
            try
            {
                saveSW.Restart();
                if (!HasChanges)
                {
                    Log($"Merge Cache: No changes detected, skipping save.");
                    return;
                }

                ModTekCacheStorage.CompressedWriteTo(persistentSets.ToList(), PersistentFilePath);
                Log($"Merge Cache: Saved to {PersistentFilePath}.");
                HasChanges = false;
            }
            catch (Exception e)
            {
                Log($"Merge Cache: Couldn't write to {PersistentFilePath}", e);
            }
            finally
            {
                saveSW.Stop();
                LogIfSlow(saveSW);
            }
        }

        internal bool HasMergedContentCached(VersionManifestEntry entry, bool fetchContent, out string cachedContent)
        {
            cachedContent = null;
            var key = new CacheKey(entry);
            if (!tempSets.TryGetValue(key, out var temp))
            {
                // lets find and fix un-typed sets
                // TODO this way a good idea? we ignore all untyped if we find one typed.. so no
                var noTypeKey = new CacheKey(null, entry.Id);
                if (!tempSets.TryGetValue(noTypeKey, out temp))
                {
                    return false;
                }

                tempSets.Remove(noTypeKey);
                temp.SetCachedPath(entry);
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
                    cachedContent = ModTekCacheStorage.CompressedStringReadFrom(temp.CachedAbsolutePath);
                }
                else if (!ModTekCacheStorage.CompressedExists(temp.CachedAbsolutePath))
                {
                    return false;
                }
            }
            catch
            {
                Log($"Merge Cache: Couldn't read cached merge result at {temp.CachedAbsolutePath}");
                return false;
            }

            return true;
        }

        internal void MergeAndCacheContent(VersionManifestEntry entry, ref string content)
        {
            if (content == null)
            {
                return;
            }

            var key = new CacheKey(entry);
            if (!tempSets.TryGetValue(key, out var temp))
            {
                return;
            }

            try
            {
                content = temp.Merge(content);
            }
            catch (Exception e)
            {
                Log($"Merge Cache: Couldn't merge {temp.CachedAbsolutePath}", e);
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(temp.CachedAbsolutePath) ?? throw new InvalidOperationException());
                ModTekCacheStorage.CompressedStringWriteTo(temp.CachedAbsolutePath, content);
                persistentSets[key] = temp;
                HasChanges = true;
            }
            catch (Exception e)
            {
                Log($"Merge Cache: Couldn't write cached merge result to {temp.CachedAbsolutePath}", e);
            }
        }

        internal bool HasMerges(VersionManifestEntry entry)
        {
            var key = new CacheKey(entry);
            return tempSets.ContainsKey(key);
        }

        internal bool AddModEntry(ModEntry entry)
        {
            if (entry.Type == BTConstants.CustomType_AdvancedJSONMerge)
            {
                var advMerge = AdvancedJSONMerge.FromFile(entry.AbsolutePath);
                if (advMerge == null)
                {
                    return true;
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
                    return true;
                }

                foreach (var target in targets)
                {
                    var copy = entry.copy();
                    copy.Id = target;
                    copy.Type = advMerge.TargetType;
                    AddTemp(entry);
                }

                return true;
            }

            if (entry.ShouldMergeJSON || entry.ShouldAppendText)
            {
                if (entry.ShouldMergeJSON && entry.IsJson || entry.ShouldAppendText && (entry.IsTxt || entry.IsCsv))
                {
                    AddTemp(entry);
                }
                else
                {
                    Log($"\tError: ShouldMergeJSON requires .json and ShouldAppendText requires .txt or .csv: \"{entry.RelativePathToMods}\".");
                }

                return true;
            }

            return false;
        }

        private void AddTemp(ModEntry entry)
        {
            var key = new CacheKey(entry);
            if (!tempSets.TryGetValue(key, out var set))
            {
                set = new MergeCacheEntry(entry);
                tempSets[key] = set;
            }

            set.Add(entry);
        }

        internal void CleanCache(out bool flagForRebuild, List<CacheKey> requestLoad)
        {
            flagForRebuild = false;
            foreach (var kv in tempSets)
            {
                if (persistentSets.TryGetValue(kv.Key, out var value) && value.Equals(kv.Value))
                {
                    value.CacheHit = true;
                }
                else
                {
                    requestLoad.Add(kv.Key);
                }
            }
            foreach (var kv in persistentSets.ToList())
            {
                if (kv.Value.CacheHit)
                {
                    continue;
                }

                persistentSets.Remove(kv.Key);
                var resourceType = BTConstants.ResourceType(kv.Key.Type);
                if (!resourceType.HasValue || BTConstants.MDDTypes.All(x => x != resourceType.Value))
                {
                    continue;
                }

                flagForRebuild = true;
                break;
            }
        }
    }
}
