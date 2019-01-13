using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModTek
{
    using static Logger;

    internal class MergeCache
    {
        public Dictionary<string, CacheEntry> CachedEntries { get; set; } = new Dictionary<string, CacheEntry>();

        /// <summary>
        ///     Gets (from the cache) or creates (and adds to cache) a JSON merge
        /// </summary>
        /// <param name="absolutePath">The path to the original JSON file</param>
        /// <param name="mergePaths">A list of the paths to merged in JSON</param>
        /// <returns>A path to the cached JSON that contains the original JSON with the mod merges applied</returns>
        public string GetOrCreateCachedEntry(string absolutePath, List<string> mergePaths)
        {
            absolutePath = Path.GetFullPath(absolutePath);
            var relativePath = ModTek.GetRelativePath(absolutePath, ModTek.GameDirectory);

            Log("");

            if (!CachedEntries.ContainsKey(relativePath) || !CachedEntries[relativePath].MatchesPaths(absolutePath, mergePaths))
            {
                var cachedAbsolutePath = Path.GetFullPath(Path.Combine(ModTek.CacheDirectory, relativePath));
                var cachedEntry = new CacheEntry(cachedAbsolutePath, absolutePath, mergePaths);

                if (cachedEntry.HasErrors)
                    return null;

                CachedEntries[relativePath] = cachedEntry;

                Log($"Merge performed: {Path.GetFileName(absolutePath)}");
            }
            else
            {
                Log($"Cached merge: {Path.GetFileName(absolutePath)} ({File.GetLastWriteTime(CachedEntries[relativePath].CacheAbsolutePath).ToString("G")})");
            }

            Log($"\t{relativePath}");

            foreach (var contributingPath in mergePaths)
                Log($"\t{ModTek.GetRelativePath(contributingPath, ModTek.ModsDirectory)}");

            Log("");

            CachedEntries[relativePath].CacheHit = true;
            return CachedEntries[relativePath].CacheAbsolutePath;
        }

        public bool HasCachedEntry(string originalPath, List<string> mergePaths)
        {
            var relativePath = ModTek.GetRelativePath(originalPath, ModTek.GameDirectory);
            return CachedEntries.ContainsKey(relativePath) && CachedEntries[relativePath].MatchesPaths(originalPath, mergePaths);
        }

        /// <summary>
        ///     Writes the cache to disk to the path, after cleaning up old entries
        /// </summary>
        /// <param name="path">Where the cache should be written to</param>
        public void WriteCacheToDisk(string path)
        {
            // remove all of the cache that we didn't use
            var unusedMergePaths = new List<string>();
            foreach (var cachedEntryKVP in CachedEntries)
                if (!cachedEntryKVP.Value.CacheHit)
                    unusedMergePaths.Add(cachedEntryKVP.Key);

            if (unusedMergePaths.Count > 0)
                Log($"");

            foreach (var unusedMergePath in unusedMergePaths)
            {
                var cacheAbsolutePath = CachedEntries[unusedMergePath].CacheAbsolutePath;
                CachedEntries.Remove(unusedMergePath);

                if (File.Exists(cacheAbsolutePath))
                    File.Delete(cacheAbsolutePath);

                Log($"Old Merge Deleted: {cacheAbsolutePath}");

                var directory = Path.GetDirectoryName(cacheAbsolutePath);
                while (Directory.Exists(directory) && Directory.GetDirectories(directory).Length == 0 && Directory.GetFiles(directory).Length == 0 && Path.GetFullPath(directory) != ModTek.CacheDirectory)
                {
                    Directory.Delete(directory);
                    Log($"Old Merge folder deleted: {directory}");
                    directory = Path.GetFullPath(Path.Combine(directory, ".."));
                }
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        /// <summary>
        /// Updates all absolute path'd cache entries to use a relative path instead
        /// </summary>
        public void UpdateToRelativePaths()
        {
            var toRemove = new List<string>();
            var toAdd = new Dictionary<string, CacheEntry>();

            foreach (var path in CachedEntries.Keys)
            {
                if (Path.IsPathRooted(path))
                {
                    var relativePath = ModTek.GetRelativePath(path, ModTek.GameDirectory);

                    toAdd[relativePath] = CachedEntries[path];
                    toRemove.Add(path);

                    toAdd[relativePath].CachePath = ModTek.GetRelativePath(toAdd[relativePath].CachePath, ModTek.GameDirectory);
                    foreach (var merge in toAdd[relativePath].Merges)
                        merge.Path = ModTek.GetRelativePath(merge.Path, ModTek.GameDirectory);
                }
            }

            foreach (var addKVP in toAdd)
                CachedEntries.Add(addKVP.Key, addKVP.Value);

            foreach (var path in toRemove)
                CachedEntries.Remove(path);
        }

        internal class CacheEntry
        {
            public string CachePath { get; set; }
            public DateTime OriginalTime { get; set; }
            public List<PathTimeTuple> Merges { get; set; } = new List<PathTimeTuple>();

            [JsonIgnore] internal string CacheAbsolutePath
            {
                get
                {
                    if (string.IsNullOrEmpty(_cacheAbsolutePath))
                        _cacheAbsolutePath = ModTek.ResolvePath(CachePath, ModTek.GameDirectory);

                    return _cacheAbsolutePath;
                }
            }
            [JsonIgnore] private string _cacheAbsolutePath;
            [JsonIgnore] internal bool CacheHit; // default is false
            [JsonIgnore] internal string ContainingDirectory;
            [JsonIgnore] internal bool HasErrors; // default is false


            [JsonConstructor]
            public CacheEntry()
            {
            }

            public CacheEntry(string cacheAbsolutePath, string originalAbsolutePath, List<string> mergePaths)
            {
                _cacheAbsolutePath = cacheAbsolutePath;
                CachePath = ModTek.GetRelativePath(cacheAbsolutePath, ModTek.GameDirectory);
                ContainingDirectory = Path.GetDirectoryName(cacheAbsolutePath);
                OriginalTime = File.GetLastWriteTimeUtc(originalAbsolutePath);

                if (string.IsNullOrEmpty(ContainingDirectory))
                {
                    HasErrors = true;
                    return;
                }

                // get the parent JSON
                JObject parentJObj;
                try
                {
                    parentJObj = ModTek.ParseGameJSONFile(originalAbsolutePath);
                }
                catch (Exception e)
                {
                    Log($"\tParent JSON at path {originalAbsolutePath} has errors preventing any merges!");
                    Log($"\t\t{e.Message}");
                    HasErrors = true;
                    return;
                }

                foreach (var mergePath in mergePaths)
                    Merges.Add(new PathTimeTuple(ModTek.GetRelativePath(mergePath, ModTek.GameDirectory), File.GetLastWriteTimeUtc(mergePath)));

                Directory.CreateDirectory(ContainingDirectory);

                using (var writer = File.CreateText(cacheAbsolutePath))
                {
                    // merge all of the merges
                    foreach (var mergePath in mergePaths)
                    {
                        JObject mergeJObj;
                        try
                        {
                            mergeJObj = ModTek.ParseGameJSONFile(mergePath);
                        }
                        catch (Exception e)
                        {
                            Log($"\tMod merge JSON at path {originalAbsolutePath} has errors preventing any merges!");
                            Log($"\t\t{e.Message}");
                            continue;
                        }

                        if (AdvancedJSONMerger.IsAdvancedJSONMerge(mergeJObj))
                        {
                            try
                            {
                                AdvancedJSONMerger.ProcessInstructionsJObject(parentJObj, mergeJObj);
                                continue;
                            }
                            catch (Exception e)
                            {
                                Log($"\tMod advanced merge JSON at path {mergePath} has errors preventing advanced json merges!");
                                Log($"\t\t{e.Message}");
                            }
                        }

                        // assume standard merging
                        parentJObj.Merge(mergeJObj, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                    }

                    // write the merged onto file to disk
                    var jsonWriter = new JsonTextWriter(writer)
                    {
                        Formatting = Formatting.Indented
                    };
                    parentJObj.WriteTo(jsonWriter);
                    jsonWriter.Close();
                }
            }

            internal bool MatchesPaths(string originalPath, List<string> mergePaths)
            {
                // must have an existing cached json
                if (!File.Exists(CacheAbsolutePath))
                    return false;

                // must have the same original file
                if (File.GetLastWriteTimeUtc(originalPath) != OriginalTime)
                    return false;

                // must match number of merges
                if (mergePaths.Count != Merges.Count)
                    return false;

                // if all paths match with write times, we match
                for (var index = 0; index < mergePaths.Count; index++)
                {
                    var mergeAbsolutePath = mergePaths[index];
                    var mergeTime = File.GetLastWriteTimeUtc(mergeAbsolutePath);
                    var cachedMergeAboslutePath = ModTek.ResolvePath(Merges[index].Path, ModTek.GameDirectory);
                    var cachedMergeTime = Merges[index].Time;

                    if (mergeAbsolutePath != cachedMergeAboslutePath || mergeTime != cachedMergeTime)
                        return false;
                }

                return true;
            }

            internal class PathTimeTuple
            {
                public PathTimeTuple(string path, DateTime time)
                {
                    Path = path;
                    Time = time;
                }

                public string Path { get; set; }
                public DateTime Time { get; set; }
            }
        }
    }
}
