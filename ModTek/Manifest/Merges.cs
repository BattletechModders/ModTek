using System.Collections.Generic;
using System.IO;
using Harmony;
using ModTek.Caches;
using ModTek.Logging;
using ModTek.Misc;
using ModTek.Mods;
using ModTek.UI;

namespace ModTek.Manifest
{
    internal static class Merges
    {
        internal static void AddMerge(string type, string id, string path)
        {
            if (!ModTek.merges.ContainsKey(type))
            {
                ModTek.merges[type] = new Dictionary<string, List<string>>();
            }

            if (!ModTek.merges[type].ContainsKey(id))
            {
                ModTek.merges[type][id] = new List<string>();
            }

            if (ModTek.merges[type][id].Contains(path))
            {
                return;
            }

            ModTek.merges[type][id].Add(path);
        }

        internal static void RemoveMerge(string type, string id)
        {
            if (!ModTek.merges.ContainsKey(type) || !ModTek.merges[type].ContainsKey(id))
            {
                return;
            }

            ModTek.merges[type].Remove(id);
            Logger.Log((string) $"\t\tHad merges for {id} but had to toss, since original file is being replaced");
        }

        internal static IEnumerator<ProgressReport> MergeFilesLoop()
        {
            // there are no mods loaded, just return
            if (ModDefsDatabase.ModLoadOrder == null || ModDefsDatabase.ModLoadOrder.Count == 0)
            {
                yield break;
            }

            // perform merges into cache
            Logger.Log((string) "\nDoing merges...");
            yield return new ProgressReport(1, "Merging", "", true);

            var mergeCache = MergeCache.FromFile(FilePaths.MergeCachePath);
            mergeCache.UpdateToRelativePaths();

            // progress panel setup
            var mergeCount = 0;
            var numEntries = 0;
            ModTek.merges.Do(pair => numEntries += pair.Value.Count);

            foreach (var type in ModTek.merges.Keys)
            {
                foreach (var id in ModTek.merges[type].Keys)
                {
                    var existingEntry = ModsManifest.FindEntry(type, id);
                    if (existingEntry == null)
                    {
                        Logger.Log((string) $"\tWarning: Have merges for {id} but cannot find an original file! Skipping.");
                        continue;
                    }

                    var originalPath = Path.GetFullPath(existingEntry.FilePath);
                    var mergePaths = ModTek.merges[type][id];

                    if (!mergeCache.HasCachedEntry(originalPath, mergePaths))
                    {
                        yield return new ProgressReport(mergeCount++ / (float) numEntries, "Merging", id);
                    }

                    var cachePath = mergeCache.GetOrCreateCachedEntry(originalPath, mergePaths);

                    // something went wrong (the parent json prob had errors)
                    if (cachePath == null)
                    {
                        continue;
                    }

                    var cacheEntry = new ModEntry(cachePath) { ShouldAppendText = false, ShouldMergeJSON = false, Type = existingEntry.Type, Id = id };

                    ModsManifest.AddModEntry(cacheEntry);
                }
            }

            mergeCache.ToFile(FilePaths.MergeCachePath);
        }
    }
}
