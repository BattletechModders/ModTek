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

        // additional temp stuff
        private readonly Dictionary<string, MergeCacheEntry> idToUntypedSet = new(); // stuff in here needs to be promoted to tempSets
        private readonly Dictionary<string, MergeCacheEntry> idToTypedSet = new(); // the first with a type claims this

        private static bool HasChanges;
        private readonly TypeResolver typeResolver;

        internal MergeCache()
        {
            PersistentFilePath = Path.Combine(PersistentDirPath, "merge_cache.json");
            typeResolver = new TypeResolver(this);

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
            if (!typeResolver.TryGetTempAndPromoteTypeless(entry, out var key, out var temp))
            {
                return false;
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
            if (entry.Type == null)
            {
                // check if already resolved
                if (idToTypedSet.TryGetValue(entry.Id, out var typedSet))
                {
                    typedSet.Add(entry);
                }
                else // otherwise add to untyped set
                {
                    if (!idToUntypedSet.TryGetValue(entry.Id, out var untypedSet))
                    {
                        untypedSet = new MergeCacheEntry();
                    }
                    idToUntypedSet.Add(entry.Id, untypedSet);
                }
            }
            else
            {
                var key = new CacheKey(entry);

                // if temp set exists, someone else already promoted any untyped
                if (tempSets.TryGetValue(key, out var set))
                {
                    set.Add(entry);
                    return;
                }

                // if untyped found, use that, otherwise create new
                if (idToUntypedSet.TryGetValue(entry.Id, out set))
                {
                    idToUntypedSet.Remove(entry.Id);
                    idToTypedSet.Add(entry.Id, set);
                }
                else
                {
                    set = new MergeCacheEntry();
                    if (!idToTypedSet.ContainsKey(entry.Id))
                    {
                        idToTypedSet.Add(entry.Id, set);
                    }
                }
                set.SetCachedPath(entry);
                set.Add(entry);
                tempSets[key] = set;
            }
        }

        internal void CleanCacheWithCompleteManifest(ref bool flagForRebuild, HashSet<CacheKey> requestLoad)
        {
            // find all still typeless and merge with typed
            foreach (var kv in tempSets.ToList())
            {
                var key = kv.Key;
                if (key.Type != null)
                {
                    continue;
                }

                typeResolver.SetTypeViaBTRL(kv.Value, ref key);
            }

            // find entries missing in cache
            foreach (var kv in tempSets.ToList())
            {
                var key = kv.Key;
                if (key.Type == null)
                {
                    continue;
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

                var resourceType = BTConstants.ResourceType(kv.Key.Type);
                if (!resourceType.HasValue || BTConstants.MDDTypes.All(x => x != resourceType.Value))
                {

                    continue;
                }

                flagForRebuild = true;
                break;
            }
        }

        private class TypeResolver
        {
            private readonly MergeCache mergeCache;

            public TypeResolver(MergeCache mergeCache)
            {
                this.mergeCache = mergeCache;
            }

            internal bool TryGetTempAndPromoteTypeless(VersionManifestEntry manifestEntryWithType, out CacheKey key, out MergeCacheEntry temp)
            {
                key = new CacheKey(null, manifestEntryWithType.Id);

                // find and promote typeless, otherwise just find direct cache key
                if (mergeCache.tempSets.TryGetValue(key, out temp))
                {
                    SetTypeViaManifestEntry(temp, manifestEntryWithType, ref key);
                }
                else
                {
                    key = new CacheKey(manifestEntryWithType);
                    if (!mergeCache.tempSets.TryGetValue(key, out temp))
                    {
                        return false;
                    }
                }

                return true;
            }

            internal bool SetTypeViaBTRL(MergeCacheEntry cacheEntry, ref CacheKey key)
            {
                var entriesById = BetterBTRL.Instance.EntriesByID(key.Id);

                if (entriesById == null || entriesById.Length < 1)
                {
                    Log($"MergeCache: Couldn't resolve type for {key.Id}, entry missing in manifest.");
                    return false;
                }
                if (entriesById.Length > 1)
                {
                    Log($"MergeCache: Couldn't resolve type for {key.Id}, multiple types found for same id in manifest, please specify type.");
                    return false;
                }

                var manifestEntry = entriesById.First();
                SetTypeViaManifestEntry(cacheEntry, manifestEntry, ref key);
                return true;
            }

            internal void SetTypeViaManifestEntry(MergeCacheEntry cacheEntry, VersionManifestEntry manifestEntry, ref CacheKey key)
            {
                cacheEntry.SetCachedPath(manifestEntry);
                mergeCache.tempSets.Remove(key);
                key = new CacheKey(manifestEntry);
                cacheEntry.SetCachedPath(manifestEntry);
                mergeCache.tempSets[key] = cacheEntry;
                Log($"MergeCache: Resolved type {key.Type} for {key.Id}.");
            }
        }
    }
}
