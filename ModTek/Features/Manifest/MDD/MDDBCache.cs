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

        private CacheDB CachedItems { get; }
        internal static bool HasChanges;

        internal MDDBCache()
        {
            PersistentFilePath = Path.Combine(PersistentDirPath, "database_cache.json");

            if (ModTekCacheStorage.CompressedExists(PersistentFilePath) && File.Exists(ModMDDBPath))
            {
                try
                {
                    CachedItems = ModTekCacheStorage.CompressedReadFrom<List<CacheKeyValue>>(PersistentFilePath)
                        .ToDictionary(kv => kv.Key, kv => kv.Value);
                    MetadataDatabase.ReloadFromDisk();
                    Log("MDDBCache: Loaded.");
                    return;
                }
                catch (Exception e)
                {
                    Log("MDDBCache: Loading db cache failed -- will rebuild it.", e);
                }
            }

            CachedItems = new CacheDB();
            Reset();
        }

        private void Reset()
        {
            FileUtils.CleanDirectory(PersistentDirPath);

            File.Copy(MDDBPath, ModMDDBPath);

            // create a new one if it doesn't exist or couldn't be added
            Log("MDDBCache: Copying over DB and rebuilding cache.");
            CachedItems.Clear();
            MetadataDatabase.ReloadFromDisk();
        }

        private readonly Stopwatch saveSW = new Stopwatch();
        internal void Save()
        {
            try
            {
                saveSW.Restart();
                if (!HasChanges)
                {
                    Log($"MDDBCache: No changes detected, skipping save.");
                    return;
                }
                MetadataDatabase.SaveMDDToPath();

                ModTekCacheStorage.CompressedWriteTo(CachedItems.ToList(), PersistentFilePath);
                Log($"MDDBCache: Saved to {PersistentFilePath}.");
                HasChanges = false;
            }
            catch (Exception e)
            {
                Log($"MDDBCache: Couldn't write mddb cache to {PersistentFilePath}", e);
            }
            finally
            {
                saveSW.Stop();
                LogIfSlow(saveSW);
            }
        }

        private readonly Stopwatch sw = new Stopwatch();
        internal void Add(VersionManifestEntry loadedEntry, string content)
        {
            if (!BTConstants.MDDBTypes.Contains(loadedEntry.Type))
            {
                return;
            }

            if (loadedEntry.IsInDefaultMDDB())
            {
                return;
            }

            if (!IsQueued(loadedEntry, out var key))
            {
                return;
            }

            if (CachedItems.TryGetValue(key, out var cachedItem))
            {
                if (cachedItem.Contains(loadedEntry))
                {
                    return;
                }
            }

            sw.Start();
            try
            {
                Log($"MDDBCache: Indexing {loadedEntry.ToShortString()}");
                MDDBIndexer.InstantiateResourceAndUpdateMDDB(loadedEntry, content);
            }
            catch (Exception e)
            {
                Log($"MDDBCache: Exception when indexing {loadedEntry.ToShortString()}", e);
            }
            sw.Stop();
            LogIfSlow(sw, "InstantiateResourceAndUpdateMDDB", 10000); // every 10s log total and reset
            if (!loadedEntry.IsInDefaultMDDB())
            {
                CachedItems[key] = FileVersionTuple.From(loadedEntry);
            }
            HasChanges = true;
        }

        private readonly HashSet<CacheKey> QueuedItems = new HashSet<CacheKey>();

        internal void IndexCustomResources(List<VersionManifestEntry> queuedResources)
        {
            // TODO get rid of blocking loop with a ReadAllText
            foreach (var entry in queuedResources)
            {
                if (!IsQueued(entry, out _))
                {
                    continue;
                }

                try
                {
                    var json = File.ReadAllText(entry.FilePath);
                    Add(entry, json);
                }
                catch (Exception e)
                {
                    Log($"ERROR: Couldn't process custom resource {entry.ToShortString()}", e);
                }
            }
        }

        internal void AddToQueueIfIndexable(ModEntry entry)
        {
            if (!BTConstants.MDDBTypes.Contains(entry.Type))
            {
                return;
            }

            var key = new CacheKey(entry);
            QueuedItems.Add(key);
        }

        private bool IsQueued(VersionManifestEntry entry, out CacheKey key)
        {
            key = new CacheKey(entry);
            return QueuedItems.Contains(key);
        }

        internal void CleanCacheWithCompleteManifest(ref bool flagForRebuild, HashSet<CacheKey> preloadResources)
        {
            if (!flagForRebuild)
            {
                // find entries missing in cache
                foreach (var type in BTConstants.MDDBTypes)
                {
                    foreach (var manifestEntry in BetterBTRL.Instance.AllEntriesOfType(type, true))
                    {
                        // these can never be missing
                        if (manifestEntry.IsInDefaultMDDB())
                        {
                            continue;
                        }

                        if (!IsQueued(manifestEntry, out var key))
                        {
                            continue;
                        }

                        // see if it is already cached
                        if (CachedItems.TryGetValue(key, out var cachedEntry))
                        {
                            cachedEntry.CacheHit = true;
                            if (!cachedEntry.Contains(manifestEntry))
                            {
                                Log($"MDDBCache: {key} outdated in cache.");
                                preloadResources.Add(key);
                            }
                        }
                        else
                        {
                            Log($"MDDBCache: {key} missing in cache.");
                            preloadResources.Add(key);
                        }
                    }
                }

                // find entries that shouldn't be in cache (anymore)
                foreach (var kv in CachedItems)
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
                Reset();
            }
        }
    }
}
