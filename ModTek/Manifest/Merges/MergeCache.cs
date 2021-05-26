using System;
using System.Collections.Generic;
using System.IO;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;
using static ModTek.Logging.Logger;

namespace ModTek.Manifest.Merges
{
    internal class MergeCache
    {
        private Dictionary<string, CacheEntry> CachedEntries { get; set; } = new();

        public string GetOrCreateCachedEntry(string absolutePath, List<string> mergePaths)
        {
            absolutePath = Path.GetFullPath(absolutePath);
            var relativePath = FileUtils.GetRelativePath(absolutePath, FilePaths.GameDirectory);

            Log("");

            if (!CachedEntries.ContainsKey(relativePath) || !CachedEntries[relativePath].MatchesPaths(absolutePath, mergePaths))
            {
                var cachedAbsolutePath = Path.GetFullPath(Path.Combine(FilePaths.CacheDirectory, relativePath));
                var cachedEntry = new CacheEntry(cachedAbsolutePath, absolutePath, mergePaths);

                if (cachedEntry.HasErrors)
                {
                    return null;
                }

                CachedEntries[relativePath] = cachedEntry;

                Log($"Merge performed: {Path.GetFileName(absolutePath)}");
            }
            else
            {
                Log($"Cached merge: {Path.GetFileName(absolutePath)} ({File.GetLastWriteTime(CachedEntries[relativePath].CacheAbsolutePath):G})");
            }

            Log($"\t{relativePath}");

            foreach (var contributingPath in mergePaths)
            {
                Log($"\t{FileUtils.GetRelativePath(contributingPath, FilePaths.GameDirectory)}");
            }

            Log("");

            CachedEntries[relativePath].CacheHit = true;
            return CachedEntries[relativePath].CacheAbsolutePath;
        }

        public bool HasCachedEntry(string originalPath, List<string> mergePaths)
        {
            var relativePath = FileUtils.GetRelativePath(originalPath, FilePaths.GameDirectory);
            return CachedEntries.ContainsKey(relativePath) && CachedEntries[relativePath].MatchesPaths(originalPath, mergePaths);
        }

        public void ToFile(string path)
        {
            // remove all of the cache that we didn't use
            var unusedMergePaths = new List<string>();
            foreach (var cachedEntryKVP in CachedEntries)
            {
                if (!cachedEntryKVP.Value.CacheHit)
                {
                    unusedMergePaths.Add(cachedEntryKVP.Key);
                }
            }

            if (unusedMergePaths.Count > 0)
            {
                Log("");
            }

            foreach (var unusedMergePath in unusedMergePaths)
            {
                var cacheAbsolutePath = CachedEntries[unusedMergePath].CacheAbsolutePath;
                CachedEntries.Remove(unusedMergePath);

                if (File.Exists(cacheAbsolutePath))
                {
                    File.Delete(cacheAbsolutePath);
                }

                Log($"Old Merge Deleted: {cacheAbsolutePath}");

                var directory = Path.GetDirectoryName(cacheAbsolutePath);
                while (Directory.Exists(directory) && Directory.GetDirectories(directory).Length == 0 && Directory.GetFiles(directory).Length == 0 && Path.GetFullPath(directory) != FilePaths.CacheDirectory)
                {
                    Directory.Delete(directory);
                    Log($"Old Merge folder deleted: {directory}");
                    directory = Path.GetFullPath(Path.Combine(directory, ".."));
                }
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public void UpdateToRelativePaths()
        {
            var toRemove = new List<string>();
            var toAdd = new Dictionary<string, CacheEntry>();

            foreach (var path in CachedEntries.Keys)
            {
                if (Path.IsPathRooted(path))
                {
                    var relativePath = FileUtils.GetRelativePath(path, FilePaths.GameDirectory);

                    toAdd[relativePath] = CachedEntries[path];
                    toRemove.Add(path);

                    toAdd[relativePath].CachePath = FileUtils.GetRelativePath(toAdd[relativePath].CachePath, FilePaths.GameDirectory);
                    foreach (var merge in toAdd[relativePath].Merges)
                    {
                        merge.Path = FileUtils.GetRelativePath(merge.Path, FilePaths.GameDirectory);
                    }
                }
            }

            foreach (var addKVP in toAdd)
            {
                CachedEntries.Add(addKVP.Key, addKVP.Value);
            }

            foreach (var path in toRemove)
            {
                CachedEntries.Remove(path);
            }
        }

        public static MergeCache FromFile(string path)
        {
            MergeCache mergeCache;

            if (File.Exists(path))
            {
                try
                {
                    mergeCache = JsonConvert.DeserializeObject<MergeCache>(File.ReadAllText(path));
                    Log("Loaded merge cache.");
                    return mergeCache;
                }
                catch (Exception e)
                {
                    LogException("Loading merge cache failed -- will rebuild it.", e);
                }
            }

            // create a new one if it doesn't exist or couldn't be added'
            Log("Building new Merge Cache.");
            mergeCache = new MergeCache();
            return mergeCache;
        }
    }
}
