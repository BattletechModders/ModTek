using System;
using System.Collections.Generic;
using System.IO;
using BattleTech;
using BattleTech.Data;
using ModTek.Misc;
using Newtonsoft.Json;
using static ModTek.Logging.Logger;
using CacheKey = System.Tuple<string, string>;
using CacheDB = System.Collections.Generic.Dictionary<System.Tuple<string, string>, ModTek.Manifest.FileVersionTuple>;

namespace ModTek.Manifest.MDD
{
    internal class MDDBCache
    {
        private static string PersistentFilePath => FilePaths.MDDBCachePath;
        private static string MDDBPath => FilePaths.MDDBPath;
        private static string ModMDDBPath => FilePaths.ModMDDBPath;

        private readonly HashSet<CacheKey> ignored = new();

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
                    Log("Loaded db cache.");
                    return;
                }
                catch (Exception e)
                {
                    LogException("Loading db cache failed -- will rebuild it.", e);
                }
            }

            // delete mod db if it exists the cache does not
            if (File.Exists(ModMDDBPath))
            {
                File.Delete(ModMDDBPath);
            }

            File.Copy(MDDBPath, ModMDDBPath);

            // create a new one if it doesn't exist or couldn't be added
            Log("Copying over DB and building new DB Cache.");
            Entries = new CacheDB();
            MetadataDatabase.ReloadFromDisk();
        }

        // TODO when to call? when do we know everything was loaded?
        internal void Save()
        {
            if (!HasChanges)
            {
                return;
            }
            try
            {
                MetadataDatabase.SaveMDDToPath();
                File.WriteAllText(PersistentFilePath, JsonConvert.SerializeObject(Entries, Formatting.Indented));
                HasChanges = false;
            }
            catch (Exception e)
            {
                Log($"Couldn't write merge cache to {PersistentFilePath}", e);
            }
        }

        internal void Add(VersionManifestEntry entry, string content, bool updateOnlyIfCacheOutdated = true)
        {
            var type = BTResourceUtils.ResourceType(entry.Type);
            if (!type.HasValue)
            {
                Log($"Internal error: {entry.Id} has invalid type: {entry.Type}");
                return;
            }

            var key = new CacheKey(entry.Type, entry.Id);
            if (ignored.Contains(key))
            {
                return;
            }

            if (updateOnlyIfCacheOutdated && Entries.TryGetValue(key, out var existingEntry) && existingEntry.UpdatedOn == entry.UpdatedOn)
            {
                return;
            }

            MetadataDatabase.Instance.InstantiateResourceAndUpdateMDDB(type.Value, entry.Id, content);
            Entries.Add(key, FileVersionTuple.From(entry));
            HasChanges = true;
        }

        internal void Ignore(ModEntry entry)
        {
            var key = new CacheKey(entry.Type, entry.Id);
            ignored.Add(key);
        }
    }
}
