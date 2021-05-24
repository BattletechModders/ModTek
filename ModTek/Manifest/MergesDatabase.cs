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
    internal static class MergesDatabase
    {
        internal static void AddMerge(string type, string id, string path)
        {
            if (!merges.ContainsKey(type))
            {
                merges[type] = new Dictionary<string, List<string>>();
            }

            if (!merges[type].ContainsKey(id))
            {
                merges[type][id] = new List<string>();
            }

            if (merges[type][id].Contains(path))
            {
                return;
            }

            merges[type][id].Add(path);
        }

        internal static void RemoveMerge(string type, string id)
        {
            if (!merges.ContainsKey(type) || !merges[type].ContainsKey(id))
            {
                return;
            }

            merges[type].Remove(id);
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
            CollectionExtensions.Do<KeyValuePair<string, Dictionary<string, List<string>>>>(merges, pair => numEntries += pair.Value.Count);

            foreach (var type in merges.Keys)
            {
                foreach (var id in merges[type].Keys)
                {
                    var existingEntry = ModsManifest.FindEntry(type, id);
                    if (existingEntry == null)
                    {
                        Logger.Log((string) $"\tWarning: Have merges for {id} but cannot find an original file! Skipping.");
                        continue;
                    }

                    var originalPath = Path.GetFullPath(existingEntry.FilePath);
                    var mergePaths = merges[type][id];

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

        internal static Dictionary<string, Dictionary<string, List<string>>> merges = new();
    }
}
