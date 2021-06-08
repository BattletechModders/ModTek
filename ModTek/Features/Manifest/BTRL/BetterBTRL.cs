using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using ModTek.Features.CustomResources;
using ModTek.Util;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.BTRL
{
    internal class BetterBTRL
    {
        public static readonly BetterBTRL Instance = new();

        private ContentPackIndex packIndex;
        private readonly TypedManifest currentManifest = new();

        private readonly VersionManifest defaultManifest;
        private readonly List<VersionManifestAddendum> hbsAddendums = new();
        private readonly List<ModAddendumManifest> orderedModAddendumManifests = new();
        private readonly Dictionary<string, List<VersionManifestEntry>> addendumEntryOverrides = new();

        private bool HasChanges;

        internal void TryFinalizeDataLoad(ContentPackIndex contentPackIndex)
        {
            SetContentPackIndex(contentPackIndex);
            if (contentPackIndex.AllContentPacksLoaded())
            {
                ContentPackManifestsLoaded();
            }
        }

        private void ContentPackManifestsLoaded()
        {
            currentManifest.DumpToDisk();

            var contentPacks = packIndex?.GetOwnedContentPacks();
            LogIf(contentPacks != null && contentPacks.Count > 0, "Owned content packs: " + contentPacks.AsTextList());

            var modsWithRequirements = orderedModAddendumManifests
                .Where(x => x.RequiredContentPacks != null && x.RequiredContentPacks.Length > 0)
                .ToList();
            LogIf(modsWithRequirements.Count > 0, "Mod addendums requiring content packs:" + modsWithRequirements
                .Select(x => $"{x.Addendum.Name} requires: {string.Join(",", x.RequiredContentPacks)}")
                .AsTextList()
            );

            ModsManifest.VerifyCaches();
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

        internal VersionManifestEntry[] AllEntriesOfType(string type)
        {
            return currentManifest.AllEntriesOfType(type);
        }

        internal VersionManifestEntry EntryByIDAndType(string id, string type)
        {
            return currentManifest.EntryByIDAndType(id, type);
        }

        private void RemoveAddendumForMemoryStore(VersionManifestAddendum addendum)
        {
            hbsAddendums.RemoveAll(x => addendum.Name.Equals(x.Name));
            HasChanges = true;
            RefreshTypedEntries();
        }

        // methods order is same as dnSpy lists them

        public void SetContentPackIndex(ContentPackIndex contentPackIndex)
        {
            packIndex = contentPackIndex;
            HasChanges = true;
            RefreshTypedEntries();
        }

        public void ApplyAddendum(VersionManifestAddendum addendum)
        {
            if (ContainsAddendum(addendum.Name))
            {
                return;
            }
            hbsAddendums.Add(addendum);
            HasChanges = true;
            RefreshTypedEntries();
        }

        private bool ContainsAddendum(string name)
        {
            return hbsAddendums.Any(x => name.Equals(x.Name));
        }

        public void AddModAddendum(ModAddendumManifest modManifest)
        {
            orderedModAddendumManifests.Add(modManifest);
            HasChanges = true;
        }

        public void RemoveAddendum(VersionManifestAddendum addendum)
        {
            // only used internally by BTRL
            // we dont support removal, since we cannot remove base types from MDDB anyway
            throw new NotImplementedException();
        }

        public VersionManifestAddendum GetAddendumByName(string name)
        {
            return hbsAddendums.FirstOrDefault(x => x.Name == name);
        }

        #region memory stores

        // this region is copy pasted from original and kept same except for some calls to Manifest and BTRL itself

        private Dictionary<string, VersionManifestMemoryStore> memoryStores = new();
        private Dictionary<BattleTechResourceType, Dictionary<string, List<VersionManifestMemoryStore>>> memoryStoreResourceIndex = new();

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
            SetContentPackIndex(UnityGameInstance.BattleTechGame?.DataManager?.ContentPackIndex);
        }

        private Stopwatch sw = new();
        internal void RefreshTypedEntries() // this is called way too often in vanilla cases, but not sure what depends on this
        {
            if (!HasChanges) // it changes all the time anyway
            {
                return;
            }

            sw.Start();
            currentManifest.Reset(defaultManifest.Entries, packIndex);
            var ownedContentPacks = packIndex?.GetOwnedContentPacks() ?? new List<string>();

            foreach (var addendum in hbsAddendums)
            {
                currentManifest.AddAddendum(ApplyOverrides(addendum));
            }

            foreach (var modAddendum in orderedModAddendumManifests)
            {
                if (modAddendum.RequiredContentPacks != null && modAddendum.RequiredContentPacks.Except(ownedContentPacks).Any())
                {
                    continue;
                }

                currentManifest.AddAddendum(ApplyOverrides(modAddendum.Addendum));
            }
            sw.Stop();
            LogIfSlow(sw);
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
            return addendum.Entries.Where(x => x.Type == type.ToString() && (!filterByOwnership || packIndex.IsResourceOwned(x.Id))).ToArray();
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
