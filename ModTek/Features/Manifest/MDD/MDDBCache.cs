using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using ModTek.Features.Manifest.BTRL;
using ModTek.Misc;
using ModTek.Util;
using static ModTek.Features.Logging.MTLogger;
using CacheDB = System.Collections.Generic.Dictionary<ModTek.Features.Manifest.CacheKey, ModTek.Features.Manifest.FileVersionTuple>;
using CacheKeyValue = System.Collections.Generic.KeyValuePair<ModTek.Features.Manifest.CacheKey, ModTek.Features.Manifest.FileVersionTuple>;

namespace ModTek.Features.Manifest.MDD
{
    internal class MDDBCache
    {
        private static string PersistentDirPath => FilePaths.MDDBCacheDirectory;
        private readonly string PersistentFilePath;

        private static string MDDBPath => FilePaths.MDDBPath;
        private static string ModMDDBPath => FilePaths.ModMDDBPath;

        private readonly HashSet<CacheKey> ignored = new();

        private CacheDB Entries { get; }
        internal static bool HasChanges;

        internal MDDBCache()
        {
            PersistentFilePath = Path.Combine(PersistentDirPath, "database_cache.json");

            if (ModTekCacheStorage.CompressedExists(PersistentFilePath) && File.Exists(ModMDDBPath))
            {
                try
                {
                    Entries = ModTekCacheStorage.CompressedReadFrom<List<CacheKeyValue>>(PersistentFilePath)
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                    MetadataDatabase.ReloadFromDisk();
                    Log("MDDB Cache: Loaded.");
                    return;
                }
                catch (Exception e)
                {
                    Log("MDDB Cache: Loading db cache failed -- will rebuild it.", e);
                }
            }

            Entries = new CacheDB();
            Reset();
        }

        private void Reset()
        {
            FileUtils.CleanDirectory(PersistentDirPath);

            File.Copy(MDDBPath, ModMDDBPath);

            // create a new one if it doesn't exist or couldn't be added
            Log("MDDB Cache: Copying over DB and rebuilding cache.");
            Entries.Clear();
            MetadataDatabase.ReloadFromDisk();
        }

        private readonly Stopwatch saveSW = new();
        internal void Save()
        {
            try
            {
                saveSW.Restart();
                if (!HasChanges)
                {
                    Log($"MDDB Cache: No changes detected, skipping save.");
                    return;
                }
                MetadataDatabase.SaveMDDToPath();

                ModTekCacheStorage.CompressedWriteTo(Entries.ToList(), PersistentFilePath);
                Log($"MDDB Cache: Saved to {PersistentFilePath}.");
                HasChanges = false;
            }
            catch (Exception e)
            {
                Log($"MDDB Cache: Couldn't write mddb cache to {PersistentFilePath}", e);
            }
            finally
            {
                saveSW.Stop();
                LogIfSlow(saveSW);
            }
        }

        private readonly Stopwatch sw = new();
        internal void Add(VersionManifestEntry entry, string content, bool updateOnlyIfCacheOutdated = false)
        {
            if (!BTConstants.ResourceType(entry.Type, out var type) || !BTConstants.MDDBTypes.Contains(type))
            {
                return;
            }

            var key = new CacheKey(entry);
            if (ignored.Contains(key))
            {
                return;
            }

            if (updateOnlyIfCacheOutdated)
            {
                if (Entries.TryGetValue(key, out var existingEntry))
                {
                    if (existingEntry.UpdatedOn == entry.UpdatedOn)
                    {
                        return;
                    }
                }
                else if (entry.IsInDefaultMDDB())
                {
                    return;
                }
            }

            sw.Start();
            MetadataDatabase.Instance.InstantiateResourceAndUpdateMDDB(type, entry.Id, content);
            sw.Stop();
            LogIfSlow(sw, "InstantiateResourceAndUpdateMDDB");
            if (!entry.IsInDefaultMDDB())
            {
                Entries.Add(key, FileVersionTuple.From(entry));
            }
            HasChanges = true;
        }

        internal void Ignore(ModEntry entry)
        {
            var key = new CacheKey(entry);
            ignored.Add(key);
        }

        internal void CleanCacheWithCompleteManifest(ref bool flagForRebuild, HashSet<CacheKey> preloadResources)
        {
            if (!flagForRebuild)
            {
                // find entries missing in cache
                foreach (var type in BTConstants.MDDBTypes)
                {
                    foreach (var manifestEntry in BetterBTRL.Instance.AllEntriesOfResource(type, true).Where(x => !x.IsInDefaultMDDB()))
                    {
                        var key = new CacheKey(manifestEntry);
                        if (Entries.TryGetValue(key, out var entry) && entry.Equals(manifestEntry))
                        {
                            entry.CacheHit = true;
                        }
                        else
                        {
                            Log($"MDDBCache: {key} missing in cache.");
                            preloadResources.Add(key);
                        }
                    }
                }

                // find entries that shouldn't be in cache (anymore)
                foreach (var kv in Entries)
                {
                    if (!kv.Value.CacheHit)
                    {
                        Log($"MDDBCache: {kv.Key} left over in cache.");
                        flagForRebuild = true;
                    }
                }
            }

            if (flagForRebuild)
            {
                Log($"MDDBCache: Rebuilding.");
                preloadResources.Clear();
                Reset();
            }
        }
    }
}
