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

        private readonly HashSet<CacheKey> QueuedItems = new HashSet<CacheKey>();

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
        internal void Add(VersionManifestEntry loadedEntry, string content, bool forceUpdate)
        {
            if (!BTConstants.ResourceType(loadedEntry.Type, out var type) || !BTConstants.MDDBTypes.Contains(type))
            {
                return;
            }

            if (!ShouldIndex(loadedEntry, out var key))
            {
                return;
            }

            if (!forceUpdate)
            {
                if (CachedItems.TryGetValue(key, out var cachedItem))
                {
                    if (cachedItem.Equals(loadedEntry))
                    {
                        return;
                    }
                }
                else if (loadedEntry.IsInDefaultMDDB())
                {
                    // if from HBS we assume no updates -> TODO how to detect changes in the future?
                    // if we have merges then we force update
                    // if we have replacement its not a default MDDB anymore
                    // TODO does not detect changed by mods -> e.g. MissionControl
                    return;
                }
            }

            sw.Start();
            try
            {
                Log($"MDDBCache: Indexing {loadedEntry.ToShortString()}");
                MetadataDatabase.Instance.InstantiateResourceAndUpdateMDDB(type, loadedEntry.Id, content);
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

        internal void AddToBeIndexed(ModEntry entry)
        {
            var key = new CacheKey(entry);
            QueuedItems.Add(key);
        }

        internal bool ShouldIndex(VersionManifestEntry entry, out CacheKey key)
        {
            key = new CacheKey(entry);
            return QueuedItems.Contains(key) || entry.IsInDefaultMDDB();
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
                        if (!ShouldIndex(manifestEntry, out var key))
                        {
                            continue;
                        }

                        if (CachedItems.TryGetValue(key, out var cachedEntry))
                        {
                            cachedEntry.CacheHit = true;
                            if (!cachedEntry.Equals(manifestEntry))
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
