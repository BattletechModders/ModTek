using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HBS.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModTek
{
    using static Logger;

    internal class MergeCache
    {
        internal class CacheEntry
        {
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

            public string CachePath { get; set; }
            public string OriginalPath { get; set; }
            public DateTime OriginalTime { get; set; }
            public List<PathTimeTuple> Merges { get; set; } = new List<PathTimeTuple>();

            [JsonIgnore]
            public bool CacheHit;

            [JsonConstructor]
            public CacheEntry() { }

            public CacheEntry(string path, string originalPath, List<string> mergePaths)
            {
                CachePath = path;
                OriginalPath = originalPath;
                OriginalTime = File.GetLastWriteTimeUtc(originalPath);

                foreach (var mergePath in mergePaths)
                {
                    Merges.Add(new PathTimeTuple(mergePath, File.GetLastWriteTimeUtc(mergePath)));
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                using (var writer = File.CreateText(path))
                {
                    // use reflection to get HBS's strip method
                    var originalText = File.ReadAllText(originalPath);

                    var type = typeof(JSONSerializationUtility);
                    var stripMethod = type.GetMethod("StripHBSCommentsFromJSON", BindingFlags.NonPublic | BindingFlags.Static);
                    var commentsStripped = stripMethod.Invoke(null, new object[]{ originalText }) as string;

                    // add missing commas
                    var rgx = new Regex(@"(\]|\}|""|[A-Za-z0-9])\s*\n\s*(\[|\{|"")", RegexOptions.Singleline);
                    var commasAdded = rgx.Replace(commentsStripped, "$1,\n$2");

                    var parentJObj = JObject.Parse(commasAdded);

                    // merge all of the merges
                    foreach (var mergePath in mergePaths)
                    {
                        var mergeJObj = JObject.Parse(File.ReadAllText(mergePath));
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

            public bool MatchesPaths(string originalPath, List<string> mergePaths)
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
        }

        public Dictionary<string, CacheEntry> CachedEntries { get; set; } = new Dictionary<string, CacheEntry>();
        
        public string GetOrCreateCachedEntry(string originalPath, List<string> mergePaths)
        {
            originalPath = Path.GetFullPath(originalPath);

            if (!CachedEntries.ContainsKey(originalPath) || !CachedEntries[originalPath].MatchesPaths(originalPath, mergePaths))
            {
                // create new cache entry; substring is to get rid of the path seperator -.-
                string cachePath = Path.GetFullPath(Path.Combine(ModTek.CacheDirectory, originalPath.Replace(ModTek.GameDirectory, "").Substring(1)));
                CachedEntries[originalPath] = new CacheEntry(cachePath, originalPath, mergePaths);

                Log($"\tMerge performed: {Path.GetFileName(originalPath)}. Now cached.");
            }
            else
            {
                Log($"\tLoaded cached merge: {Path.GetFileName(originalPath)}.");
            }

            CachedEntries[originalPath].CacheHit = true;
            return CachedEntries[originalPath].CachePath;
        }

        public void WriteCacheToDisk(string path)
        {
            // remove all of the cache that we didn't use
            var unusedMergePaths = new List<string>();

            foreach (var cachedEntry in CachedEntries)
            {
                if (!cachedEntry.Value.CacheHit)
                    unusedMergePaths.Add(cachedEntry.Key);
            }

            foreach (var unusedMergePath in unusedMergePaths)
            {
                var cachePath = CachedEntries[unusedMergePath].CachePath;
                CachedEntries.Remove(unusedMergePath);

                if(File.Exists(cachePath))
                    File.Delete(cachePath);

                if(Directory.GetFiles(Path.GetDirectoryName(cachePath)).Length == 0)
                    Directory.Delete(Path.GetDirectoryName(cachePath));
            }
            
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
