using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BattleTech;
using ModTek.Features.AdvJSONMerge;
using ModTek.Misc;
using Newtonsoft.Json;
using static ModTek.Logging.Logger;
using MergeSets = System.Collections.Generic.Dictionary<string, ModTek.Features.Manifest.Merges.MergeCacheEntry>;

namespace ModTek.Features.Manifest.Merges
{
    internal class MergeCache
    {
        private static string PersistentFilePath => FilePaths.MergeCachePath;

        private readonly MergeSets persistentSets; // stuff in here was merged
        private readonly MergeSets tempSets = new(); // stuff in here has merges queued
        private static bool HasChanges;

        internal MergeCache()
        {
            if (File.Exists(PersistentFilePath))
            {
                try
                {
                    persistentSets = JsonConvert.DeserializeObject<MergeSets>(File.ReadAllText(PersistentFilePath));
                    return;
                }
                catch (Exception e)
                {
                    Log("Merge Cache: Loading merge cache failed.", e);
                }
            }

            // create a new one if it doesn't exist or couldn't be added'
            Log("Merge Cache: Rebuilding cache.");
            persistentSets = new MergeSets();
        }

        private readonly Stopwatch saveSW = new();
        internal void Save()
        {
            try
            {
                saveSW.Restart();
                if (!HasChanges)
                {
                    Log($"Merge Cache: No changes detected, skipping save.");
                    return;
                }

                var json = JsonConvert.SerializeObject(persistentSets, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(PersistentFilePath, json);
                Log($"Merge Cache: Saved to {PersistentFilePath}.");
                HasChanges = false;
            }
            catch (Exception e)
            {
                Log($"Merge Cache: Couldn't write to {PersistentFilePath}", e);
            }
            finally
            {
                saveSW.Stop();
                LogIfSlow(saveSW);
            }
        }

        internal bool HasMergedContentCached(VersionManifestEntry entry, bool fetchContent, out string cachedContent)
        {
            cachedContent = null;
            var key = CacheKeys.Unique(entry);
            if (!tempSets.TryGetValue(key, out var temp))
            {
                // lets find and fix un-typed sets
                // TODO this way a good idea? we ignore all untyped if we find one typed.. so no
                var noTypeKey = CacheKeys.Unique(null, entry.Id);
                if (!tempSets.TryGetValue(noTypeKey, out temp))
                {
                    return false;
                }

                tempSets.Remove(noTypeKey);
                temp.SetCachedPath(entry);
                tempSets[key] = temp;
            }

            temp.OriginalUpdatedOn = entry.UpdatedOn;

            if (!persistentSets.TryGetValue(key, out var persist))
            {
                return false;
            }

            if (!temp.Equals(persist))
            {
                return false;
            }

            try
            {
                if (fetchContent)
                {
                    cachedContent = File.ReadAllText(temp.CachedAbsolutePath);
                }
                else if (!File.Exists(temp.CachedAbsolutePath))
                {
                    return false;
                }
            }
            catch
            {
                Log($"Merge Cache: Couldn't read cached merge result at {temp.CachedAbsolutePath}");
                return false;
            }

            return true;
        }

        internal string MergeAndCacheContent(VersionManifestEntry entry, string originalContent)
        {
            if (originalContent == null)
            {
                return null;
            }

            var key = CacheKeys.Unique(entry);
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
                Log($"Merge Cache: Couldn't merge {temp.CachedAbsolutePath}", e);
                return null;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(temp.CachedAbsolutePath) ?? throw new InvalidOperationException());
                File.WriteAllText(temp.CachedAbsolutePath, mergedContent);
                persistentSets[key] = temp;
                HasChanges = true;
            }
            catch (Exception e)
            {
                Log($"Merge Cache: Couldn't write cached merge result to {temp.CachedAbsolutePath}", e);
            }

            return mergedContent;
        }

        internal bool HasMerges(VersionManifestEntry entry)
        {
            var key = CacheKeys.Unique(entry);
            return tempSets.ContainsKey(key);
        }

        internal bool AddModEntry(ModEntry entry)
        {
            if (entry.Type == BTConstants.CustomType_AdvancedJSONMerge)
            {
                var advMerge = AdvancedJSONMerge.FromFile(entry.AbsolutePath);
                if (advMerge == null)
                {
                    return true;
                }

                var targets = new List<string>();
                if (!string.IsNullOrEmpty(advMerge.TargetID))
                {
                    targets.Add(advMerge.TargetID);
                }

                if (advMerge.TargetIDs != null)
                {
                    targets.AddRange(advMerge.TargetIDs);
                }

                if (targets.Count == 0)
                {
                    Log($"\tError: AdvancedJSONMerge: \"{entry.RelativePathToMods}\" didn't target any IDs. Skipping merge.");
                    return true;
                }

                foreach (var target in targets)
                {
                    var copy = entry.copy();
                    copy.Id = target;
                    copy.Type = advMerge.TargetType;
                    AddTemp(entry);
                }

                return true;
            }

            if (entry.ShouldMergeJSON || entry.ShouldAppendText)
            {
                if (entry.ShouldMergeJSON && entry.IsJson || entry.ShouldAppendText && (entry.IsTxt || entry.IsCsv))
                {
                    AddTemp(entry);
                }
                else
                {
                    Log($"\tError: ShouldMergeJSON requires .json and ShouldAppendText requires .txt or .csv: \"{entry.RelativePathToMods}\".");
                }

                return true;
            }

            return false;
        }

        private void AddTemp(ModEntry entry)
        {
            var key = CacheKeys.Unique(entry);
            if (!tempSets.TryGetValue(key, out var set))
            {
                set = new MergeCacheEntry(entry);
                tempSets[key] = set;
            }

            set.Add(entry);
        }
    }
}
