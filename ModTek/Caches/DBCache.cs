using System;
using System.Collections.Generic;
using System.IO;
using ModTek.Util;
using Newtonsoft.Json;
using static ModTek.Util.Logger;

namespace ModTek.Caches
{
    internal class DBCache
    {
        public Dictionary<string, DateTime> Entries { get; }

        public DBCache(string path, string mddbPath, string modMDDBPath)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path) && File.Exists(modMDDBPath))
            {
                try
                {
                    Entries = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(File.ReadAllText(path));
                    Log("Loaded db cache.");
                    return;
                }
                catch (Exception e)
                {
                    LogException("Loading db cache failed -- will rebuild it.", e);
                }
            }

            // delete mod db if it exists the cache does not
            if (File.Exists(modMDDBPath))
            {
                File.Delete(modMDDBPath);
            }

            File.Copy(mddbPath, modMDDBPath);

            // create a new one if it doesn't exist or couldn't be added
            Log("Copying over DB and building new DB Cache.");
            Entries = new Dictionary<string, DateTime>();
        }

        public void UpdateToRelativePaths()
        {
            var toRemove = new List<string>();
            var toAdd = new Dictionary<string, DateTime>();

            foreach (var path in Entries.Keys)
            {
                if (!Path.IsPathRooted(path))
                {
                    continue;
                }

                var relativePath = FileUtils.GetRelativePath(path, ModTek.GameDirectory);
                toAdd[relativePath] = Entries[path];
                toRemove.Add(path);
            }

            foreach (var addKVP in toAdd)
            {
                Entries.Add(addKVP.Key, addKVP.Value);
            }

            foreach (var path in toRemove)
            {
                Entries.Remove(path);
            }
        }

        public void ToFile(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(Entries, Formatting.Indented));
        }
    }
}
