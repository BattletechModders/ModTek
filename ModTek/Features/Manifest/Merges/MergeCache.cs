using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.AdvJSONMerge;
using ModTek.Features.Manifest.BTRL;
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
            if (!PromoteUntypedIfPossible(key, out var temp))
            {
                if (!tempSets.TryGetValue(key, out temp))
                {
                    return false;
                }
            }

            temp.SetCachedPathAndUpdatedOn(entry);

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
                    AddTemp(copy);
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
            if (entry.Type == null)
            {
                // check if id already mapped to a type
                if (idToTypedSet.TryGetValue(entry.Id, out var typedSet))
                {
                    typedSet.Add(entry);
                }
                else // otherwise add to untyped set
                {
                    if (!idToUntypedSet.TryGetValue(entry.Id, out var untypedSet))
                    {
                        untypedSet = new MergeCacheEntry();
                        idToUntypedSet[entry.Id] = untypedSet;
                    }
                    untypedSet.Add(entry);
                }
            }
            else
            {
                var key = new CacheKey(entry);

                // if temp set exists, someone else already promoted any untyped
                if (tempSets.TryGetValue(key, out var temp))
                {
                    temp.Add(entry);
                    return;
                }

                if (!PromoteUntypedIfPossible(key, out temp))
                {
                    temp = new MergeCacheEntry();
                    if (!idToTypedSet.ContainsKey(entry.Id))
                    {
                        idToTypedSet.Add(entry.Id, temp);
                    }
                    tempSets[key] = temp;
                }

                temp.SetCachedPath(entry);
                temp.Add(entry);
            }
        }

        internal void CleanCacheWithCompleteManifest(ref bool flagForRebuild, HashSet<CacheKey> requestLoad)
        {
            PromoteAllUntypedIfPossible();

            // find entries missing in cache
            foreach (var kv in tempSets.ToList())
            {
                var key = kv.Key;
                if (key.Type == null)
                {
                    continue;
                }

                if (kv.Value.OriginalUpdatedOn == null)
                {
                    if (BTConstants.ResourceType(kv.Key.Type, out var resourceType))
                    {
                        var manifestEntry = BetterBTRL.Instance.EntryByID(kv.Key.Id, resourceType);
                        kv.Value.SetCachedPathAndUpdatedOn(manifestEntry);
                    }
                }

                if (persistentSets.TryGetValue(key, out var value) && value.Equals(kv.Value))
                {
                    value.CacheHit = true;
                }
                else
                {
                    Log($"MergeCache: {key} missing in cache.");
                    requestLoad.Add(key);
                }
            }

            // find entries that shouldn't be in cache (anymore)
            foreach (var kv in persistentSets.ToList())
            {
                if (kv.Value.CacheHit)
                {
                    continue;
                }

                persistentSets.Remove(kv.Key);
                try
                {
                    if (File.Exists(kv.Value.CachedAbsolutePath))
                    {
                        File.Delete(kv.Value.CachedAbsolutePath);
                    }
                }
                catch (Exception e)
                {
                    Log($"MergeCache: Error when deleting cached file for {kv.Key}", e);
                }

                Log($"MergeCache: {kv.Key} left over in cache.");

                if (!BTConstants.ResourceType(kv.Key.Type, out var resourceType)
                    || BTConstants.MDDTypes.All(x => x != resourceType))
                {
                    continue;
                }

                flagForRebuild = true;
                break;
            }
        }

        #region untyped

        // additional temp stuff
        private readonly Dictionary<string, MergeCacheEntry> idToUntypedSet = new(); // stuff in here needs to be promoted to tempSets
        private readonly Dictionary<string, MergeCacheEntry> idToTypedSet = new(); // the first with a type claims this

        private bool PromoteUntypedIfPossible(CacheKey key, out MergeCacheEntry entry)
        {
            if (idToUntypedSet.TryGetValue(key.Id, out entry))
            {
                PromoteUntyped(key, entry);
                return true;
            }
            return false;
        }

        private void PromoteUntyped(CacheKey key, MergeCacheEntry entry)
        {
            idToUntypedSet.Remove(key.Id);
            idToTypedSet.Add(key.Id, entry);
            tempSets[key] = entry;
        }

        private void PromoteAllUntypedIfPossible()
        {
            foreach (var kv in idToUntypedSet.ToList())
            {
                SetTypeViaBTRLIfPossible(kv.Key, kv.Value);
            }
        }

        private bool SetTypeViaBTRLIfPossible(string id, MergeCacheEntry cacheEntry)
        {
            var entriesById = BetterBTRL.Instance.EntriesByID(id);

            if (entriesById == null || entriesById.Length < 1)
            {
                Log($"MergeCache: Couldn't resolve type for {id}, entry missing in manifest.");
                return false;
            }
            if (entriesById.Length > 1)
            {
                Log($"MergeCache: Couldn't resolve type for {id}, multiple types found for same id in manifest, please specify type.");
                return false;
            }

            var manifestEntry = entriesById.First();
            var key = new CacheKey(manifestEntry.Type, manifestEntry.Id);
            PromoteUntyped(key, cacheEntry);
            cacheEntry.SetCachedPathAndUpdatedOn(manifestEntry);

            //Log($"MergeCache: Mapped {key.Id} to type {key.Type}.");
            return true;
        }

        #endregion
    }
}
