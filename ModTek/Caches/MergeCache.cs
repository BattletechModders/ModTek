using System;
using System.Collections.Generic;
using System.IO;
using ModTek.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static ModTek.Util.Logger;

namespace ModTek.Caches
{
    internal class MergeCache
    {
        public Dictionary<string, CacheEntry> CachedEntries { get; set; } = new Dictionary<string, CacheEntry>();

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
                Log($"Cached merge: {Path.GetFileName(absolutePath)} ({File.GetLastWriteTime(CachedEntries[relativePath].CacheAbsolutePath):G})");
            }

            Log($"\t{relativePath}");

            foreach (var contributingPath in mergePaths)
                Log($"\t{ModTek.GetRelativePath(contributingPath, ModTek.GameDirectory)}");

            Log("");

            CachedEntries[relativePath].CacheHit = true;
            return CachedEntries[relativePath].CacheAbsolutePath;
        }

        public bool HasCachedEntry(string originalPath, List<string> mergePaths)
        {
            var relativePath = ModTek.GetRelativePath(originalPath, ModTek.GameDirectory);
            return CachedEntries.ContainsKey(relativePath) && CachedEntries[relativePath].MatchesPaths(originalPath, mergePaths);
        }

        public void ToFile(string path)
        {
            // remove all of the cache that we didn't use
            var unusedMergePaths = new List<string>();
            foreach (var cachedEntryKVP in CachedEntries)
            {
                if (!cachedEntryKVP.Value.CacheHit)
                    unusedMergePaths.Add(cachedEntryKVP.Key);
            }

            if (unusedMergePaths.Count > 0)
                Log("");

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

        internal class CacheEntry
        {
            [JsonIgnore] private string cacheAbsolutePath;
            [JsonIgnore] internal bool CacheHit; // default is false
            [JsonIgnore] internal string ContainingDirectory;
            [JsonIgnore] internal bool HasErrors; // default is false
            public string CachePath { get; set; }
            public DateTime OriginalTime { get; set; }
            public List<PathTimeTuple> Merges { get; set; } = new List<PathTimeTuple>();

            [JsonIgnore]
            internal string CacheAbsolutePath
            {
                get
                {
                    if (string.IsNullOrEmpty(cacheAbsolutePath))
                        cacheAbsolutePath = ModTek.ResolvePath(CachePath, ModTek.GameDirectory);

                    return cacheAbsolutePath;
                }
            }

            [JsonConstructor]
            public CacheEntry()
            {
            }

            public CacheEntry(string absolutePath, string originalAbsolutePath, List<string> mergePaths)
            {
                cacheAbsolutePath = absolutePath;
                CachePath = ModTek.GetRelativePath(absolutePath, ModTek.GameDirectory);
                ContainingDirectory = Path.GetDirectoryName(absolutePath);
                OriginalTime = File.GetLastWriteTimeUtc(originalAbsolutePath);

                if (string.IsNullOrEmpty(ContainingDirectory))
                {
                    HasErrors = true;
                    return;
                }

                foreach (var mergePath in mergePaths)
                    Merges.Add(new PathTimeTuple(ModTek.GetRelativePath(mergePath, ModTek.GameDirectory), File.GetLastWriteTimeUtc(mergePath)));

                Directory.CreateDirectory(ContainingDirectory);

                // do json merge if json
                if (Path.GetExtension(absolutePath)?.ToLowerInvariant() == ".json")
                {
                    // get the parent JSON
                    JObject parentJObj;
                    try
                    {
                        parentJObj = ModTek.ParseGameJSONFile(originalAbsolutePath);
                    }
                    catch (Exception e)
                    {
                        LogException($"\tParent JSON at path {originalAbsolutePath} has errors preventing any merges!", e);
                        HasErrors = true;
                        return;
                    }

                    using (var writer = File.CreateText(absolutePath))
                    {
                        // merge all of the merges
                        foreach (var mergePath in mergePaths)
                        {
                            try
                            {
                                // since all json files are opened and parsed before this point, they won't have errors
                                JSONMerger.MergeIntoTarget(parentJObj, ModTek.ParseGameJSONFile(mergePath));
                            }
                            catch (Exception e)
                            {
                                LogException($"\tMod JSON merge at path {ModTek.GetRelativePath(mergePath, ModTek.GameDirectory)} has errors preventing merge!", e);
                            }
                        }

                        // write the merged onto file to disk
                        var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
                        parentJObj.WriteTo(jsonWriter);
                        jsonWriter.Close();
                    }

                    return;
                }

                // do file append if not json
                using (var writer = File.CreateText(absolutePath))
                {
                    writer.Write(File.ReadAllText(originalAbsolutePath));

                    foreach (var mergePath in mergePaths)
                        writer.Write(File.ReadAllText(mergePath));
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
                    var cachedMergeAbsolutePath = ModTek.ResolvePath(Merges[index].Path, ModTek.GameDirectory);
                    var cachedMergeTime = Merges[index].Time;

                    if (mergeAbsolutePath != cachedMergeAbsolutePath || mergeTime != cachedMergeTime)
                        return false;
                }

                return true;
            }

            internal class PathTimeTuple
            {
                public string Path { get; set; }
                public DateTime Time { get; set; }

                public PathTimeTuple(string path, DateTime time)
                {
                    Path = path;
                    Time = time;
                }
            }
        }
    }
}
