using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Data;
using Harmony;
using ModTek.CustomTypes;
using ModTek.Logging;
using ModTek.Misc;
using ModTek.Mods;
using ModTek.UI;
using ModTek.Util;

namespace ModTek.Manifest.MDD
{
    internal static class MDDHelper
    {
        private static bool AddModEntryToDB(MetadataDatabase db, DBCache dbCache, string absolutePath, string typeStr)
        {
            if (Path.GetExtension(absolutePath)?.ToLowerInvariant() != ".json")
            {
                return false;
            }

            var type = (BattleTechResourceType) Enum.Parse(typeof(BattleTechResourceType), typeStr);
            var relativePath = FileUtils.GetRelativePath(absolutePath, FilePaths.GameDirectory);

            switch (type) // switch is to avoid poisoning the output_log.txt with known types that don't use MDD
            {
                case BattleTechResourceType.TurretDef:
                case BattleTechResourceType.UpgradeDef:
                case BattleTechResourceType.VehicleDef:
                case BattleTechResourceType.ContractOverride:
                case BattleTechResourceType.SimGameEventDef:
                case BattleTechResourceType.LanceDef:
                case BattleTechResourceType.MechDef:
                case BattleTechResourceType.PilotDef:
                case BattleTechResourceType.WeaponDef:
                    var writeTime = File.GetLastWriteTimeUtc(absolutePath);
                    if (!dbCache.Entries.ContainsKey(relativePath) || dbCache.Entries[relativePath] != writeTime)
                    {
                        try
                        {
                            VersionManifestHotReload.InstantiateResourceAndUpdateMDDB(type, absolutePath, db);

                            // don't write game files to the dbCache, since they're assumed to be default in the db
                            if (!absolutePath.Contains(FilePaths.StreamingAssetsDirectory))
                            {
                                dbCache.Entries[relativePath] = writeTime;
                            }

                            return true;
                        }
                        catch (Exception e)
                        {
                            Logger.LogException($"\tError: Add to DB failed for {Path.GetFileName(absolutePath)}, exception caught:", e);
                            return false;
                        }
                    }

                    break;
            }

            return false;
        }

        internal static IEnumerator<ProgressReport> AddToDBLoop()
        {
            Logger.Log("\nSyncing Database...");
            yield return new ProgressReport(1, "Syncing Database", "", true);

            var dbCache = new DBCache(FilePaths.DBCachePath, FilePaths.MDDBPath, FilePaths.ModMDDBPath);
            dbCache.UpdateToRelativePaths();

            // since DB instance is read at type init, before we patch the file location
            // need re-init the mddb to read from the proper modded location
            var mddbTraverse = Traverse.Create(typeof(MetadataDatabase));
            mddbTraverse.Field("instance").SetValue(null);
            mddbTraverse.Method("InitInstance").GetValue();

            // check if files removed from DB cache
            var shouldWriteDB = false;
            var shouldRebuildDB = false;
            var replacementEntries = new List<VersionManifestEntry>();
            var removeEntries = new List<string>();
            foreach (var path in dbCache.Entries.Keys)
            {
                var absolutePath = FileUtils.ResolvePath(path, FilePaths.GameDirectory);

                if (ModsManifest.IsBTRLEntryCached(absolutePath))
                {
                    continue;
                }

                Logger.Log($"\tNeed to remove DB entry from file in path: {path}");

                // file is missing, check if another entry exists with same filename in manifest or in BTRL entries
                var fileName = Path.GetFileName(path);
                var existingEntry = ModsManifest.FindEntryByFileName(fileName);

                // TODO: DOES NOT HANDLE CASE WHERE REMOVING VANILLA CONTENT IN DB

                if (existingEntry == null)
                {
                    Logger.Log("\t\tHave to rebuild DB, no existing entry in VersionManifest matches removed entry");
                    shouldRebuildDB = true;
                    break;
                }

                replacementEntries.Add(existingEntry);
                removeEntries.Add(path);
            }

            // add removed entries replacements to db
            if (!shouldRebuildDB)
            {
                // remove old entries
                foreach (var removeEntry in removeEntries)
                {
                    dbCache.Entries.Remove(removeEntry);
                }

                foreach (var replacementEntry in replacementEntries)
                {
                    if (AddModEntryToDB(MetadataDatabase.Instance, dbCache, Path.GetFullPath(replacementEntry.FilePath), replacementEntry.Type))
                    {
                        Logger.Log($"\t\tReplaced DB entry with an existing entry in path: {FileUtils.GetRelativePath(replacementEntry.FilePath, FilePaths.GameDirectory)}");
                        shouldWriteDB = true;
                    }
                }
            }

            // if an entry has been removed and we cannot find a replacement, have to rebuild the mod db
            if (shouldRebuildDB)
            {
                dbCache = new DBCache(null, FilePaths.MDDBPath, FilePaths.ModMDDBPath);
            }

            Logger.Log("\nAdding dynamic enums:");
            var addCount = 0;
            var mods = ModDefsDatabase.ModsInLoadOrder();

            foreach (var moddef in mods)
            {
                if (moddef.DataAddendumEntries.Count != 0)
                {
                    Logger.Log($"{moddef.Name}:");
                    foreach (var dataAddendumEntry in moddef.DataAddendumEntries)
                    {
                        if (AddendumUtils.LoadDataAddendum(dataAddendumEntry, moddef.Directory))
                        {
                            shouldWriteDB = true;
                        }

                        yield return new ProgressReport(addCount / (float) mods.Count, "Populating Dynamic Enums", moddef.Name);
                    }
                }

                ++addCount;
            }

            // add needed files to db
            addCount = 0;
            var entries = ModsManifest.GetAddToDbEntries();
            foreach (var modEntry in entries)
            {
                if (AddModEntryToDB(MetadataDatabase.Instance, dbCache, modEntry.Path, modEntry.Type))
                {
                    yield return new ProgressReport(addCount / (float) entries.Count, "Populating Database", modEntry.Id);
                    Logger.Log($"\tAdded/Updated {modEntry.Id} ({modEntry.Type})");
                    shouldWriteDB = true;
                }

                addCount++;
            }

            // Add any custom tags to DB
            if (ModsManifest.CustomTags.Count > 0)
            {
                Logger.Log("Processing CustomTags:");
            }

            foreach (var modEntry in ModsManifest.CustomTags)
            {
                CustomTypeProcessor.AddOrUpdateTag(modEntry.Path);
            }

            if (ModsManifest.CustomTagSets.Count > 0)
            {
                Logger.Log("Processing CustomTagSets:");
            }

            foreach (var modEntry in ModsManifest.CustomTagSets)
            {
                CustomTypeProcessor.AddOrUpdateTagSet(modEntry.Path);
            }

            //ModLoadOrder.Count;

            dbCache.ToFile(FilePaths.DBCachePath);

            if (shouldWriteDB)
            {
                yield return new ProgressReport(1, "Writing Database", "", true);
                Logger.Log("Writing DB");

                var inMemoryDB = typeof(MetadataDatabase).GetField("inMemoryDB", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(MetadataDatabase.Instance);
                Task.Run(
                    () =>
                    {
                        lock (inMemoryDB)
                        {
                            MetadataDatabase.Instance.WriteInMemoryDBToDisk();
                        }
                    }
                );
            }
        }
    }
}
