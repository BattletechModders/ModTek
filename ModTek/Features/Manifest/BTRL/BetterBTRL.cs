using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using ModTek.Features.Logging;
using ModTek.Features.Manifest.MDD;
using ModTek.Util;

namespace ModTek.Features.Manifest.BTRL
{
    internal class BetterBTRL
    {
        public static readonly BetterBTRL Instance = new BetterBTRL();

        internal readonly BetterCPI PackIndex;
        private readonly TypedManifest currentManifest;

        private readonly VersionManifest defaultManifest;
        private readonly List<VersionManifestAddendum> addendums = new List<VersionManifestAddendum>();
        private readonly List<ModAddendumManifest> orderedModAddendumManifests = new List<ModAddendumManifest>();
        private readonly VersionManifestAddendum mergeAddendum = new VersionManifestAddendum("ModTekMergeCacheAddendum");
        private readonly Dictionary<string, List<VersionManifestEntry>> addendumEntryOverrides = new Dictionary<string, List<VersionManifestEntry>>();

        private bool HasChanges;

        internal void ContentPackManifestsLoaded()
        {
            currentManifest.DumpToDisk();

            foreach (var entry in currentManifest.AllEntries(true))
            {
                if (!string.IsNullOrEmpty(entry.AssetBundleName)
                    && EntryByID(entry.AssetBundleName, BattleTechResourceType.AssetBundle) == null
                    && !entry.GetRawPath().StartsWith("Assets/Resources/UnlockedAssets"))
                {
                    MTLogger.Info.Log($"\t\tError: Cannot find referenced asset bundle {entry.AssetBundleName} by {entry.ToShortString()}.");
                }
            }

            ModsManifest.ContentPackManifestsLoaded();
        }

        internal void AddMergeManifestEntry(VersionManifestEntry entry)
        {
            mergeAddendum.Add(entry);
            HasChanges = true;
        }

        internal void AddAddendumOverrideEntry(string addendumName, VersionManifestEntry manifestEntry)
        {
            addendumEntryOverrides.GetOrCreate(addendumName).Add(manifestEntry);
            HasChanges = true;
        }

        private VersionManifestAddendum ApplyOverrides(VersionManifestAddendum addendum)
        {
            if (!addendumEntryOverrides.TryGetValue(addendum.Name, out var overrides))
            {
                return addendum;
            }

            var copy = new VersionManifestAddendum(addendum.Name);
            copy.AddRange(addendum.Entries);
            copy.AddRange(overrides); // duplicates are sorted inside TypedManifest
            return copy;
        }

        internal VersionManifestEntry[] EntriesByID(string id)
        {
            return currentManifest.EntriesByID(id);
        }

        internal VersionManifestEntry[] AllEntriesOfType(string type, bool filterByOwnership = false)
        {
            if (BTConstants.BTResourceType(type, out var resourceType))
            {
                return currentManifest.AllEntriesOfResource(resourceType, filterByOwnership);
            }
            return currentManifest.AllEntriesOfType(type);
        }

        internal VersionManifestEntry EntryByIDAndType(string id, string type, bool filterByOwnership = false)
        {
            if (BTConstants.BTResourceType(type, out var resourceType))
            {
                return currentManifest.EntryByID(id, resourceType, filterByOwnership);
            }
            return currentManifest.EntryByIDAndType(id, type);
        }

        private void RemoveAddendumForMemoryStore(VersionManifestAddendum addendum)
        {
            addendums.RemoveAll(x => addendum.Name.Equals(x.Name));
            HasChanges = true;
            RefreshTypedEntries();
        }

        // methods order is same as dnSpy lists them

        public void SetContentPackIndex(ContentPackIndex contentPackIndex)
        {
        }

        public void ApplyAddendum(VersionManifestAddendum addendum)
        {
            if (ContainsAddendum(addendum.Name))
            {
                return;
            }
            addendums.Add(addendum);
            HasChanges = true;
            RefreshTypedEntries();
        }

        private bool ContainsAddendum(string name)
        {
            return addendums.Any(x => name.Equals(x.Name));
        }

        public void AddModAddendum(ModAddendumManifest modManifest, bool forceApply)
        {
            orderedModAddendumManifests.Add(modManifest);
            HasChanges = true;
            // avoid to do a full refresh just so modded content can merge into other modded content
            if (forceApply)
            {
                currentManifest.AddAddendum(modManifest.Addendum);
            }
        }

        public void RemoveAddendum(VersionManifestAddendum addendum)
        {
            RemoveAddendum(addendum.Name);
        }

        internal void RemoveAddendum(string addendumName)
        {
            if (addendums.RemoveAll(a => a.Name == addendumName) > 0)
            {
                HasChanges = true;
                RefreshTypedEntries();
            }
        }

        public VersionManifestAddendum GetAddendumByName(string name)
        {
            return addendums.FirstOrDefault(x => x.Name == name);
        }

        #region memory stores

        // this region is copy pasted from original and kept same except for some calls to Manifest and BTRL itself

        private Dictionary<string, VersionManifestMemoryStore> memoryStores = new Dictionary<string, VersionManifestMemoryStore>();
        private Dictionary<BattleTechResourceType, Dictionary<string, List<VersionManifestMemoryStore>>> memoryStoreResourceIndex = new Dictionary<BattleTechResourceType, Dictionary<string, List<VersionManifestMemoryStore>>>();

        public void ApplyMemoryStore(VersionManifestMemoryStore memoryStore)
        {
            if (memoryStores.ContainsKey(memoryStore.Name))
            {
                return;
            }

            ApplyAddendum(memoryStore);
            memoryStores.Add(memoryStore.Name, memoryStore);
            memoryStore.SubscribeToContentsChanged(IndexMemoryStore);
            IndexMemoryStore(memoryStore);
            RefreshTypedEntries();
        }

        public void RemoveMemoryStore(VersionManifestMemoryStore memoryStore)
        {
            if (!memoryStores.ContainsKey(memoryStore.Name))
            {
                return;
            }

            RemoveAddendumForMemoryStore(memoryStore);
            memoryStores.Remove(memoryStore.Name);
            memoryStore.SubscribeToContentsChanged(IndexMemoryStore);
            UnIndexMemoryStore(memoryStore);
            RefreshTypedEntries();
        }

        private void IndexMemoryStore(VersionManifestMemoryStore memoryStore)
        {
            UnIndexMemoryStore(memoryStore);
            foreach (VersionManifestEntry versionManifestEntry in memoryStore)
            {
                var key = versionManifestEntry.Type.FromString();
                if (!memoryStoreResourceIndex.TryGetValue(key, out var dictionary))
                {
                    dictionary = new Dictionary<string, List<VersionManifestMemoryStore>>();
                    memoryStoreResourceIndex[key] = dictionary;
                }

                if (!dictionary.TryGetValue(versionManifestEntry.Id, out var list))
                {
                    list = new List<VersionManifestMemoryStore>();
                    dictionary[versionManifestEntry.Id] = list;
                }

                if (!list.Contains(memoryStore))
                {
                    list.Add(memoryStore);
                }
            }
        }

        private bool UnIndexMemoryStore(VersionManifestMemoryStore memoryStore)
        {
            foreach (var obj in memoryStore)
            {
                var versionManifestEntry = (VersionManifestEntry) obj;
                var key = versionManifestEntry.Type.FromString();
                Dictionary<string, List<VersionManifestMemoryStore>> dictionary;
                List<VersionManifestMemoryStore> list;
                if (memoryStoreResourceIndex.TryGetValue(key, out dictionary) && dictionary.TryGetValue(versionManifestEntry.Id, out list))
                {
                    return list.Remove(memoryStore);
                }
            }

            return false;
        }

        public List<VersionManifestMemoryStore> GetMemoryStoresContainingEntry(BattleTechResourceType resourceType, string id)
        {
            Dictionary<string, List<VersionManifestMemoryStore>> dictionary;
            if (!memoryStoreResourceIndex.TryGetValue(resourceType, out dictionary))
            {
                return null;
            }

            List<VersionManifestMemoryStore> result;
            if (!dictionary.TryGetValue(id, out result))
            {
                return null;
            }

            return result;
        }

        public VersionManifestMemoryStore GetMemoryStoreByName(string name)
        {
            VersionManifestMemoryStore result;
            if (!memoryStores.TryGetValue(name, out result))
            {
                return null;
            }

            return result;
        }

        #endregion

        private BetterBTRL()
        {
            PackIndex = new BetterCPI();
            currentManifest = new TypedManifest(PackIndex);

            defaultManifest = VersionManifestUtilities.ManifestFromCSV(VersionManifestUtilities.MANIFEST_FILEPATH);

            // move auto-detected addendums from inside the default manifest to BetterBTRL
            foreach (var addendum in defaultManifest.ActiveAddendums)
            {
                ApplyAddendum(addendum);
            }
            defaultManifest.ClearAddendums();

            // now apply all addendums references as CSV, doesn't seem to be used but better safe than sorry
            foreach (var entry in defaultManifest.FindAll(x => x.IsAddendum))
            {
                if (ContainsAddendum(entry.Name) || entry.IsFileAsset)
                {
                    continue;
                }

                var addendum = VersionManifestUtilities.AddendumFromCSV(entry.FilePath);
                ApplyAddendum(addendum);
            }
        }

        private Stopwatch sw = new Stopwatch();
        internal void RefreshTypedEntries() // this is called way too often in vanilla cases, but not sure what depends on this
        {
            if (!HasChanges) // it changes all the time anyway
            {
                return;
            }

            sw.Start();
            currentManifest.Reset(defaultManifest.Entries);

            foreach (var addendum in addendums)
            {
                currentManifest.AddAddendum(ApplyOverrides(addendum));
            }

            PackIndex.ClearTrackedModAddendumManifests();
            foreach (var modAddendum in orderedModAddendumManifests)
            {
                PackIndex.TrackModAddendumManifest(modAddendum);
                currentManifest.AddAddendum(ApplyOverrides(modAddendum.Addendum));
            }

            // add merge cache, make sure to only overwrite resources already in the locator
            {
                var addendum = new VersionManifestAddendum(mergeAddendum.Name);
                foreach (var entry in mergeAddendum.Entries)
                {
                    if (currentManifest.EntryByIDAndType(entry.Id, entry.Type) != null)
                    {
                        addendum.Add(entry);
                    }
                }
                currentManifest.AddAddendum(addendum);
            }
            sw.Stop();
            MTLogger.Info.LogIfSlow(sw);
        }

        public VersionManifestEntry[] AllEntries()
        {
            return currentManifest.AllEntries(false);
        }

        public VersionManifestEntry[] AllEntriesOfResource(BattleTechResourceType type, bool filterByOwnership = false)
        {
            return currentManifest.AllEntriesOfResource(type, filterByOwnership);
        }

        public VersionManifestEntry[] AllEntriesOfResourceFromAddendum(BattleTechResourceType type, VersionManifestAddendum addendum, bool filterByOwnership = false)
        {
            return addendum.Entries.Where(x => x.Type == type.ToString() && (!filterByOwnership || PackIndex.IsResourceOwned(x.Id))).ToArray();
        }

        public VersionManifestEntry EntryByID(string id, BattleTechResourceType type, bool filterByOwnership = false)
        {
            return currentManifest.EntryByID(id, type, filterByOwnership);
        }

        public void RemoveEntry(VersionManifestEntry entry)
        {
            // only used by ModLoader, which we disable anyway
            // we dont support removal, since we cannot remove base types from MDDB anyway
            throw new NotImplementedException();
        }
    }
}
