using System.Collections.Generic;
using System.Diagnostics;
using BattleTech.Data;
using ModTek.Util;

namespace ModTek.Features.Manifest.BTRL;

internal class BetterCPI
{
    private List<string> OwnedContentPacks = new();
    private Dictionary<string, string> BaseResourceMap = new();
    private readonly Dictionary<string, string> ModsResourceMap = new(); // resourceId, contentPackName
    private readonly Dictionary<string, string> ModsTypeMap = new(); // resourceId, resourceType
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
            Log.Main.Info?.LogIf(OwnedContentPacks.Count > 0, "Owned content packs: " + OwnedContentPacks.AsTextList());
        }
    }

    internal void TrackModEntry(ModEntry modEntry)
    {
        ModsResourceMap[modEntry.Id] = modEntry.RequiredContentPack;
        ModsTypeMap[modEntry.Id] = modEntry.Type;
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
        else if (ModsResourceMap.TryGetValue(resourceId, out var mrmContentPackName))
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
        var sw = new Stopwatch();
        sw.Start();
        Log.Main.Debug?.Log("PatchMDD for content pack items added by mods.");
        foreach (var kv in ModsResourceMap)
        {
            var resourceId = kv.Key;
            var contentPackName = kv.Value;

            // filter out vanilla resources, those are already set and ModTek shouldn't override vanilla data
            if (BaseResourceMap.ContainsKey(resourceId))
            {
                continue;
            }

            var resourceType = ModsTypeMap[kv.Key];
            MetadataDatabase.Instance.UpdateContentPackItem(resourceType, resourceId, contentPackName);
        }
        sw.Stop();
        Log.Main.Debug?.LogIfSlow(sw, "BetterCPI.PatchMDD");
    }
}