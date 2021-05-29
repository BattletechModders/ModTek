using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using ModTek.Misc;
using Newtonsoft.Json;
using static ModTek.Logging.Logger;
using CacheDB = System.Collections.Generic.Dictionary<string, ModTek.Features.Manifest.FileVersionTuple>;

namespace ModTek.Features.Manifest.MDD
{
    internal class MDDBCache
    {
        private static string PersistentFilePath => FilePaths.MDDBCachePath;
        private static string MDDBPath => FilePaths.MDDBPath;
        private static string ModMDDBPath => FilePaths.ModMDDBPath;

        private readonly HashSet<string> ignored = new();

        private CacheDB Entries { get; }
        internal static bool HasChanges;

        internal MDDBCache()
        {
            if (!string.IsNullOrEmpty(PersistentFilePath) && File.Exists(PersistentFilePath) && File.Exists(ModMDDBPath))
            {
                try
                {
                    Entries = JsonConvert.DeserializeObject<CacheDB>(File.ReadAllText(PersistentFilePath));
                    MetadataDatabase.ReloadFromDisk();
                    Log("MDDB Cache: Loaded.");
                    return;
                }
                catch (Exception e)
                {
                    Log("MDDB Cache: Loading db cache failed -- will rebuild it.", e);
                }
            }

            // delete mod db if it exists the cache does not
            if (File.Exists(ModMDDBPath))
            {
                File.Delete(ModMDDBPath);
            }

            File.Copy(MDDBPath, ModMDDBPath);

            // create a new one if it doesn't exist or couldn't be added
            Log("MDDB Cache: Copying over DB and rebuilding cache.");
            Entries = new CacheDB();
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
                var json = JsonConvert.SerializeObject(Entries, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                File.WriteAllText(PersistentFilePath, json);
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

        internal void Add(VersionManifestEntry entry, string content, bool updateOnlyIfCacheOutdated = false)
        {
            var type = BTConstants.ResourceType(entry.Type);
            if (!type.HasValue)
            {
                Log($"MDDB Cache: Internal error: {entry.Id} has invalid type: {entry.Type}");
                return;
            }

            if (!BTConstants.MDDTypes.Contains(type.Value))
            {
                return;
            }

            var key = CacheKeys.Unique(entry);
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

            MetadataDatabase.Instance.InstantiateResourceAndUpdateMDDB(type.Value, entry.Id, content);
            Entries.Add(key, FileVersionTuple.From(entry));
            HasChanges = true;
        }

        internal void Ignore(ModEntry entry)
        {
            var key = CacheKeys.Unique(entry);
            ignored.Add(key);
        }
    }
}
