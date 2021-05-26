using System;
using System.IO;
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

        internal CacheEntry GetTemp(string bundleName, string id)
        {
            return tempSets.TryGetValue(new CacheKey(bundleName, id), out var set) ? set : null;
        }

        internal void AddTemp(string bundleName, string id, ModEntry modEntry)
        {
            var key = new CacheKey(bundleName, id);
            if (!tempSets.TryGetValue(key, out var set))
            {
                set = new CacheEntry(bundleName, id, modEntry);
                tempSets[key] = set;
            }
            set.Add(modEntry);
        }

        internal string GetCachedContent(string bundleName, string id, DateTime version)
        {
            var key = new CacheKey(bundleName, id);
            if (!tempSets.TryGetValue(key, out var temp))
            {
                return null;
            }
            temp.OriginalVersion = version;

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

        internal string MergeAndCacheContent(string bundleName, string id, DateTime version, string originalContent)
        {
            var key = new CacheKey(bundleName, id);
            if (!tempSets.TryGetValue(key, out var temp))
            {
                Log($"Internal Error: Couldn't find cache key in temp merge sets {key}");
                return null;
            }
            temp.OriginalVersion = version;
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
            Save();
            return mergedContent;
        }
    }
}
