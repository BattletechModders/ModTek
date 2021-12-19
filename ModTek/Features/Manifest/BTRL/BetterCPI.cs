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
        private Dictionary<string, string[]> ModdedResourceMap = new Dictionary<string, string[]>();
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
            ModdedResourceMap.Clear();
        }
        internal void TrackModAddendumManifest(ModAddendumManifest manifest)
        {
            if (manifest.RequiredContentPacks == null || manifest.RequiredContentPacks.Length == 0)
            {
                return;
            }
            foreach (var entry in manifest.Addendum.Entries)
            {
                // The first mod adding the resource is the one specifying the required content packs
                if (ModdedResourceMap.ContainsKey(entry.Id))
                {
                    continue;
                }
                ModdedResourceMap[entry.Id] = manifest.RequiredContentPacks;
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
            if (BaseResourceMap.TryGetValue(resourceId, out var contentPackName))
            {
                if (!OwnedContentPacks.Contains(contentPackName))
                {
                    return false;
                }
            }
            else if (ModdedResourceMap.TryGetValue(resourceId, out var contentPackNames))
            {
                var anyMissingContentPacks = contentPackNames.Except(OwnedContentPacks).Any();
                if (anyMissingContentPacks)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
