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
            public string CachePath { get; set; }
            public string OriginalPath { get; set; }
            public DateTime OriginalTimeStamp { get; set; }
            public Dictionary<string, DateTime> Merges { get; set; } = new Dictionary<string, DateTime>();

            [JsonConstructor]
            public CacheEntry() { }

            public CacheEntry(string path, string originalPath, List<string> mergePaths)
            {
                CachePath = path;
                OriginalPath = originalPath;
                OriginalTimeStamp = File.GetLastWriteTimeUtc(originalPath);

                foreach (var mergePath in mergePaths)
                {
                    Merges[mergePath] = File.GetLastWriteTimeUtc(mergePath);
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

                    Log($"Merged into {originalPath}, now cached");
                }
            }

            public bool MatchesPaths(string originalPath, List<string> paths)
            {
                if (OriginalPath != originalPath || File.GetLastWriteTimeUtc(originalPath) != OriginalTimeStamp)
                {
                    return false;
                }

                // if all paths match with write times, we match
                foreach (var path in paths)
                {
                    if (!Merges.ContainsKey(path) || Merges[path] != File.GetLastWriteTimeUtc(path))
                        return false;
                }

                // must match both ways
                foreach (var merge in Merges)
                {
                    if (!paths.Contains(merge.Key))
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
            }
            
            return CachedEntries[originalPath].CachePath;
        }
    }
}
