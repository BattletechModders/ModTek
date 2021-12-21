using System.Collections.Generic;
using System.Linq;
using BattleTech.Data;
using ModTek.Util;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.BTRL
{
    internal class BetterCPI
    {
        private List<string> OwnedContentPacks = new List<string>();
        private Dictionary<string, string> BaseResourceMap = new Dictionary<string, string>();
        private readonly Dictionary<string, string> ModResourceMap = new Dictionary<string, string>();
        private readonly List<ModAddendumManifest> ModManifests = new List<ModAddendumManifest>();
        internal bool AllContentPacksOwned; // used to speed up checks and helps during merging/indexing

        // called on ownership changes
        internal void TryFinalizeDataLoad(ContentPackIndex packIndex, Dictionary<string, string> resourceMap)
        {
            BaseResourceMap = resourceMap;
            OwnedContentPacks = packIndex.GetOwnedContentPacks();
            var AllContentPacksLoaded = packIndex.AllContentPacksLoaded();
            AllContentPacksOwned = AllContentPacksLoaded && packIndex.AreContactPacksOwned(packIndex.GetAllLoadedContentPackIds());
            if (AllContentPacksLoaded)
            {
                LogIf(OwnedContentPacks.Count > 0, "Owned content packs: " + OwnedContentPacks.AsTextList());
            }
        }

        internal void ClearTrackedModAddendumManifests()
        {
            ModResourceMap.Clear();
            ModManifests.Clear();
        }
        internal void TrackModAddendumManifest(ModAddendumManifest manifest)
        {
            if (string.IsNullOrEmpty(manifest.RequiredContentPack))
            {
                return;
            }
            ModManifests.Add(manifest);
            foreach (var entry in manifest.Addendum.Entries)
            {
                if (ModResourceMap.TryGetValue(entry.Id, out var requiredContentPack))
                {
                    if (requiredContentPack != manifest.RequiredContentPack)
                    {
                        Log($"Warning: Detected multiple entries with same resource id ({entry.Id}) but different {nameof(ModEntry.RequiredContentPack)} ({requiredContentPack} vs {manifest.RequiredContentPack}).");
                    }
                    continue;
                }
                ModResourceMap[entry.Id] = manifest.RequiredContentPack;
            }
        }

        internal bool IsResourceOwned(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId))
            {
                return false;
            }

            if (AllContentPacksOwned)
            {
                return true;
            }

            // make sure vanilla checks have precedence
            if (BaseResourceMap.TryGetValue(resourceId, out var brmContentPackName))
            {
                if (!OwnedContentPacks.Contains(brmContentPackName))
                {
                    return false;
                }
            }
            else if (ModResourceMap.TryGetValue(resourceId, out var mrmContentPackName))
            {
                if (!OwnedContentPacks.Contains(mrmContentPackName))
                {
                    return false;
                }
            }

            return true;
        }

        internal void PatchMDD(ContentPackIndex packIndex)
        {
            Log("Patching MDDB for content pack items added by mods.");
            var loadedContentPacks = packIndex.GetAllLoadedContentPackIds();
            foreach (var manifest in ModManifests)
            {
                if (!loadedContentPacks.Contains(manifest.RequiredContentPack))
                {
                    continue;
                }
                foreach (var entry in manifest.Addendum.Entries)
                {
                    if (!BTConstants.MDDBTypes.Contains(entry.Type))
                    {
                        continue;
                    }
                    MetadataDatabase.Instance.UpdateContentPackItem(entry.Type, entry.Id, manifest.RequiredContentPack);
                }
            }
        }
    }
}
