using System;
using System.IO;
using BattleTech;
using ModTek.Misc;
using Newtonsoft.Json;
using static ModTek.Logging.Logger;
using CacheKey = System.Tuple<string, string>;
using MergeSets = System.Collections.Generic.Dictionary<System.Tuple<string, string>, ModTek.Manifest.Merges.Cache.CacheEntry>;

namespace ModTek.Manifest.Merges.Cache
{
    internal class MergeCache
    {
        private static string MergeCacheFilePath => FilePaths.MergeCachePath;

        private readonly MergeSets persistentSets;
        private readonly MergeSets tempSets = new();

        internal MergeCache()
        {
            if (File.Exists(MergeCacheFilePath))
            {
                try
                {
                    persistentSets = JsonConvert.DeserializeObject<MergeSets>(File.ReadAllText(MergeCacheFilePath));
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

        private void Save()
        {
            try
            {
                File.WriteAllText(MergeCacheFilePath, JsonConvert.SerializeObject(persistentSets, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log($"Couldn't write merge cache to {MergeCacheFilePath}", e);
            }
        }

        internal void AddTemp(ModEntry modEntry)
        {
            var key = new CacheKey(modEntry.Type, modEntry.Id);
            if (!tempSets.TryGetValue(key, out var set))
            {
                set = new CacheEntry(modEntry);
                tempSets[key] = set;
            }
            set.Add(modEntry);
        }

        internal string GetCachedContent(VersionManifestEntry entry)
        {
            var key = new CacheKey(entry.Type, entry.Id);
            if (!tempSets.TryGetValue(key, out var temp))
            {
                // lets find and fix un-typed sets
                // TODO this way a good idea? well we ignore all untyped if we find one typed.. so no
                var noTypeKey = new CacheKey(null, entry.Id);
                if (!tempSets.TryGetValue(noTypeKey, out temp))
                {
                    return null;
                }

                tempSets.Remove(noTypeKey);
                temp.ResourceType = entry.Type;
                tempSets[key] = temp;
            }
            temp.UpdatedOn = entry.UpdatedOn;

            if (!persistentSets.TryGetValue(key, out var persist))
            {
                return null;
            }

            if (!temp.Equals(persist))
            {
                return null;
            }

            try
            {
                return File.ReadAllText(temp.CachedPath);
            }
            catch
            {
                Log($"Couldn't read cached merge result at {temp.CachedPath}");
                return null;
            }
        }

        internal string MergeAndCacheContent(VersionManifestEntry entry, string originalContent)
        {
            if (originalContent == null)
            {
                return null;
            }

            var key = new CacheKey(entry.Type, entry.Id);
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

            Save();// TODO only save at certain points, e.g. after datamanger finished loading a bunch of stuff
            return mergedContent;
        }
    }
}
