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
        /// <param name="originalPath">The path to the original JSON file</param>
        /// <param name="mergePaths">A list of the paths to merged in JSON</param>
        /// <returns>A path to the cached JSON that contains the original JSON with the mod merges applied</returns>
        public string GetOrCreateCachedEntry(string originalPath, List<string> mergePaths)
        {
            originalPath = Path.GetFullPath(originalPath);
            var relativePath = ModTek.GetRelativePath(originalPath, ModTek.GameDirectory);

            // remove old entries using absolute path
            if (CachedEntries.ContainsKey(originalPath))
            {
                CachedEntries[relativePath] = CachedEntries[originalPath];
                CachedEntries.Remove(originalPath);

                // clean up old entries absolute path in merges and cachepath
                CachedEntries[relativePath].CachePath = ModTek.GetRelativePath(CachedEntries[relativePath].CachePath, ModTek.GameDirectory);
                foreach (var merge in CachedEntries[relativePath].Merges)
                    merge.Path = ModTek.GetRelativePath(merge.Path, ModTek.GameDirectory);
            }

            if (!CachedEntries.ContainsKey(relativePath) || !CachedEntries[relativePath].MatchesPaths(originalPath, mergePaths))
            {
                // create new cache entry; substring is to get rid of the path seperator -.-
                var cachedAbsolutePath = Path.GetFullPath(Path.Combine(ModTek.CacheDirectory, originalPath.Replace(ModTek.GameDirectory, "").Substring(1)));
                var cachedEntry = new CacheEntry(cachedAbsolutePath, originalPath, mergePaths);

                if (cachedEntry.HasErrors)
                    return null;

                CachedEntries[relativePath] = cachedEntry;

                Log($"\tMerge performed: {Path.GetFileName(originalPath)}. Now cached.");
            }
            else
            {
                Log($"\tLoaded cached merge: {Path.GetFileName(originalPath)}.");
            }

            CachedEntries[relativePath].CacheHit = true;
            return CachedEntries[relativePath].CacheAbsolutePath;
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

            foreach (var unusedMergePath in unusedMergePaths)
            {
                var cacheAbsolutePath = CachedEntries[unusedMergePath].CacheAbsolutePath;
                CachedEntries.Remove(unusedMergePath);

                if (File.Exists(cacheAbsolutePath))
                    File.Delete(cacheAbsolutePath);

                var directory = Path.GetDirectoryName(cacheAbsolutePath);
                if (directory != null && Directory.GetFiles(directory).Length == 0)
                    Directory.Delete(directory);
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
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

            public CacheEntry(string absolutePath, string originalPath, List<string> mergePaths)
            {
                _cacheAbsolutePath = absolutePath;
                CachePath = ModTek.GetRelativePath(absolutePath, ModTek.GameDirectory);
                ContainingDirectory = Path.GetDirectoryName(absolutePath);
                OriginalTime = File.GetLastWriteTimeUtc(originalPath);

                if (string.IsNullOrEmpty(ContainingDirectory))
                {
                    HasErrors = true;
                    return;
                }

                // get the parent JSON
                JObject parentJObj;
                try
                {
                    parentJObj = ModTek.ParseGameJSON(File.ReadAllText(originalPath));
                }
                catch (Exception e)
                {
                    Log($"\tParent JSON at path {originalPath} has errors preventing any merges!");
                    Log($"\t\t{e.Message}");
                    HasErrors = true;
                    return;
                }

                foreach (var mergePath in mergePaths)
                    Merges.Add(new PathTimeTuple(ModTek.GetRelativePath(mergePath, ModTek.GameDirectory), File.GetLastWriteTimeUtc(mergePath)));

                Directory.CreateDirectory(ContainingDirectory);

                using (var writer = File.CreateText(absolutePath))
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
                            Log($"\tMod merge JSON at path {originalPath} has errors preventing any merges!");
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
