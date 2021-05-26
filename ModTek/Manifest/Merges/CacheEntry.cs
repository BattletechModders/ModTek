using System;
using System.Collections.Generic;
using System.IO;
using ModTek.Logging;
using ModTek.Manifest.AdvMerge;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModTek.Manifest.Merges
{
    internal class CacheEntry
    {
        [JsonIgnore]
        private string cacheAbsolutePath;

        [JsonIgnore]
        internal bool CacheHit; // default is false

        [JsonIgnore]
        internal string ContainingDirectory;

        [JsonIgnore]
        internal bool HasErrors; // default is false

        public string CachePath { get; set; }
        public DateTime OriginalTime { get; set; }
        public List<PathTimeTuple> Merges { get; set; } = new();

        [JsonIgnore]
        internal string CacheAbsolutePath
        {
            get
            {
                if (string.IsNullOrEmpty(cacheAbsolutePath))
                {
                    cacheAbsolutePath = FileUtils.ResolvePath(CachePath, FilePaths.GameDirectory);
                }

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
            CachePath = FileUtils.GetRelativePath(absolutePath, FilePaths.GameDirectory);
            ContainingDirectory = Path.GetDirectoryName(absolutePath);
            OriginalTime = File.GetLastWriteTimeUtc(originalAbsolutePath);

            if (string.IsNullOrEmpty(ContainingDirectory))
            {
                HasErrors = true;
                return;
            }

            foreach (var mergePath in mergePaths)
            {
                Merges.Add(new PathTimeTuple(FileUtils.GetRelativePath(mergePath, FilePaths.GameDirectory), File.GetLastWriteTimeUtc(mergePath)));
            }

            Directory.CreateDirectory(ContainingDirectory);

            // do json merge if json
            if (Path.GetExtension(absolutePath)?.ToLowerInvariant() == ".json")
            {
                // get the parent JSON
                JObject parentJObj;
                try
                {
                    parentJObj = JsonUtils.ParseGameJSON(originalAbsolutePath);
                }
                catch (Exception e)
                {
                    Logger.LogException($"\tParent JSON at path {originalAbsolutePath} has errors preventing any merges!", e);
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
                            JSONMerger.MergeIntoTarget(parentJObj, JsonUtils.ParseGameJSON(mergePath));
                        }
                        catch (Exception e)
                        {
                            Logger.LogException($"\tMod JSON merge at path {FileUtils.GetRelativePath(mergePath, FilePaths.GameDirectory)} has errors preventing merge!", e);
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
                {
                    writer.Write(File.ReadAllText(mergePath));
                }
            }
        }

        internal bool MatchesPaths(string originalPath, List<string> mergePaths)
        {
            // must have an existing cached json
            if (!File.Exists(CacheAbsolutePath))
            {
                return false;
            }

            // must have the same original file
            if (File.GetLastWriteTimeUtc(originalPath) != OriginalTime)
            {
                return false;
            }

            // must match number of merges
            if (mergePaths.Count != Merges.Count)
            {
                return false;
            }

            // if all paths match with write times, we match
            for (var index = 0; index < mergePaths.Count; index++)
            {
                var mergeAbsolutePath = mergePaths[index];
                var mergeTime = File.GetLastWriteTimeUtc(mergeAbsolutePath);
                var cachedMergeAbsolutePath = FileUtils.ResolvePath(Merges[index].Path, FilePaths.GameDirectory);
                var cachedMergeTime = Merges[index].Time;

                if (mergeAbsolutePath != cachedMergeAbsolutePath || mergeTime != cachedMergeTime)
                {
                    return false;
                }
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
