using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;

namespace ModTek.Manifest.BTRL
{
	internal class BetterBTRL
	{
        private ContentPackIndex packIndex;
        private readonly TypedManifest currentManifest = new();
        private readonly VersionManifest defaultManifest;
        private readonly List<VersionManifestAddendum> hbsAddendums = new();
        private readonly List<ModManifest> orderedModAddendums = new();

        // methods order is same as dnSpy lists them

        public void Dispose()
        {
            if (packIndex == null)
            {
                return;
            }

            var contentPackIndex = packIndex;
            contentPackIndex.contentPackLoadedCallback = (ContentPackIndex.OnContentPackLoaded)Delegate.Remove(contentPackIndex.contentPackLoadedCallback, new ContentPackIndex.OnContentPackLoaded(RefreshTypedEntries));
        }

        public void SetContentPackIndex(ContentPackIndex contentPackIndex)
        {
            packIndex = contentPackIndex;
            contentPackIndex.contentPackLoadedCallback = (ContentPackIndex.OnContentPackLoaded)Delegate.Combine(contentPackIndex.contentPackLoadedCallback, new ContentPackIndex.OnContentPackLoaded(RefreshTypedEntries));
        }

        public void ApplyAddendum(VersionManifestAddendum addendum)
        {
            hbsAddendums.RemoveAll(x => addendum.Name.Equals(x.Name));
            hbsAddendums.Add(addendum);
            RefreshTypedEntries();
        }

        // TODO should be used when mod dlls were successfully loaded
        public void AddModAddendum(VersionManifestAddendum addendum, List<string> resources)
        {
            var modManifest = new ModManifest { Addendum = addendum, RequiredOwnedResources = resources};
            orderedModAddendums.Add(modManifest);
        }

        public void RemoveAddendum(VersionManifestAddendum addendum)
        {
            hbsAddendums.RemoveAll(x => addendum.Name.Equals(x.Name));
            RefreshTypedEntries();
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

            RemoveAddendum(memoryStore);
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
			foreach (object obj in memoryStore)
			{
				VersionManifestEntry versionManifestEntry = (VersionManifestEntry)obj;
				BattleTechResourceType key = versionManifestEntry.Type.FromString();
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

        public BetterBTRL()
        {
            defaultManifest = VersionManifestUtilities.ManifestFromCSV(VersionManifestUtilities.MANIFEST_FILEPATH);
            RefreshTypedEntries();
        }

		private void RefreshTypedEntries()
        {
            currentManifest.Reset(defaultManifest.Entries);

            foreach (var addendum in hbsAddendums)
            {
                currentManifest.AddAddendum(addendum, packIndex);
            }

            foreach (var modAddendum in orderedModAddendums)
            {
                currentManifest.AddModAddendum(modAddendum);
            }
        }

        public VersionManifestEntry[] AllEntries()
        {
            // not used by anyone
            throw new NotImplementedException();
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
            return currentManifest.GetEntryByID(id, type, filterByOwnership);
        }

		public void RemoveEntry(VersionManifestEntry entry)
		{
            // only used by ModLoader, which we disable anyway
            throw new NotImplementedException();
		}
    }
}
