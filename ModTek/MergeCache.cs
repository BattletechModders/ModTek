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

            if (!CachedEntries.ContainsKey(originalPath) || !CachedEntries[originalPath].MatchesPaths(originalPath, mergePaths))
            {
                // create new cache entry; substring is to get rid of the path seperator -.-
                var cachePath = Path.GetFullPath(Path.Combine(ModTek.CacheDirectory, originalPath.Replace(ModTek.GameDirectory, "").Substring(1)));
                var cachedEntry = new CacheEntry(cachePath, originalPath, mergePaths);

                if (cachedEntry.HasErrors)
                    return null;

                CachedEntries[originalPath] = cachedEntry;

                Log($"\tMerge performed: {Path.GetFileName(originalPath)}. Now cached.");
            }
            else
            {
                Log($"\tLoaded cached merge: {Path.GetFileName(originalPath)}.");
            }

            CachedEntries[originalPath].CacheHit = true;
            return CachedEntries[originalPath].CachePath;
        }

        /// <summary>
        ///     Writes the cache to disk to the path, after cleaning up old entries
        /// </summary>
        /// <param name="path">Where the cache should be written to</param>
        public void WriteCacheToDisk(string path)
        {
            // remove all of the cache that we didn't use
            var unusedMergePaths = new List<string>();
            foreach (var cachedEntry in CachedEntries)
                if (!cachedEntry.Value.CacheHit)
                    unusedMergePaths.Add(cachedEntry.Key);

            foreach (var unusedMergePath in unusedMergePaths)
            {
                var cachePath = CachedEntries[unusedMergePath].CachePath;
                CachedEntries.Remove(unusedMergePath);

                if (File.Exists(cachePath))
                    File.Delete(cachePath);

                var directory = Path.GetDirectoryName(cachePath);
                if (directory != null && Directory.GetFiles(directory).Length == 0)
                    Directory.Delete(directory);
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        internal class CacheEntry
        {
            [JsonIgnore] internal bool CacheHit; // default is false
            [JsonIgnore] internal string ContainingDirectory;
            [JsonIgnore] internal bool HasErrors; // default is false

            [JsonConstructor]
            public CacheEntry()
            {
            }

            public CacheEntry(string path, string originalPath, List<string> mergePaths)
            {
                CachePath = path;
                ContainingDirectory = Path.GetDirectoryName(path);
                OriginalPath = originalPath;
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
                    Merges.Add(new PathTimeTuple(mergePath, File.GetLastWriteTimeUtc(mergePath)));

                Directory.CreateDirectory(ContainingDirectory);

                using (var writer = File.CreateText(path))
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

            public string CachePath { get; set; }
            public string OriginalPath { get; set; }
            public DateTime OriginalTime { get; set; }
            public List<PathTimeTuple> Merges { get; set; } = new List<PathTimeTuple>();

            internal bool MatchesPaths(string originalPath, List<string> mergePaths)
            {
                // must have an existing cached json
                if (!File.Exists(CachePath))
                    return false;

                // must have the same original file
                if (OriginalPath != originalPath || File.GetLastWriteTimeUtc(originalPath) != OriginalTime)
                    return false;

                // must match number of merges
                if (mergePaths.Count != Merges.Count)
                    return false;

                // if all paths match with write times, we match
                for (var index = 0; index < mergePaths.Count; index++)
                {
                    var mergePath = mergePaths[index];
                    var mergeTime = File.GetLastWriteTimeUtc(mergePath);
                    var cachedMergePath = Merges[index].Path;
                    var cachedMergeTime = Merges[index].Time;

                    if (mergePath != cachedMergePath || mergeTime != cachedMergeTime)
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
