using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using Harmony;
using ModTek.Manifest.BTRL;

// ReSharper disable RedundantAssignment

namespace ModTek.Manifest.Patches
{
    // fix constructor being called
    [HarmonyPatch(typeof(BattleTechResourceLocator), "RefreshTypedEntries")]
    public static class BattleTechResourceLocator_RefreshTypedEntries_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix()
        {
            return false;
        }
    }

    // well we do want the original BTRLs to properly dispose
    //[HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.Dispose))]
    // public static class BattleTechResourceLocator_Dispose_Patch
    // {
    //     public static bool Prepare()
    //     {
    //         return ModTek.Enabled;
    //     }
    //
    //     public static bool Prefix()
    //     {
    //         return false;
    //     }
    // }

    // redirect all public methods to new class

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.SetContentPackIndex))]
    public static class BattleTechResourceLocator_SetContentPackIndex_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(ContentPackIndex contentPackIndex)
        {
            BetterBTRL.Instance.SetContentPackIndex(contentPackIndex);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.ApplyAddendum))]
    public static class BattleTechResourceLocator_ApplyAddendum_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(VersionManifestAddendum addendum)
        {
            BetterBTRL.Instance.ApplyAddendum(addendum);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.RemoveAddendum))]
    public static class BattleTechResourceLocator_RemoveAddendum_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(VersionManifestAddendum addendum)
        {
            BetterBTRL.Instance.RemoveAddendum(addendum);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.GetAddendumByName))]
    public static class BattleTechResourceLocator_GetAddendumByName_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(string name, ref VersionManifestAddendum __result)
        {
            __result = BetterBTRL.Instance.GetAddendumByName(name);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.ApplyMemoryStore))]
    public static class BattleTechResourceLocator_ApplyMemoryStore_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(VersionManifestMemoryStore memoryStore)
        {
            BetterBTRL.Instance.ApplyMemoryStore(memoryStore);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.RemoveMemoryStore))]
    public static class BattleTechResourceLocator_RemoveMemoryStore_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(VersionManifestMemoryStore memoryStore)
        {
            BetterBTRL.Instance.RemoveMemoryStore(memoryStore);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.GetMemoryStoresContainingEntry))]
    public static class BattleTechResourceLocator_GetMemoryStoresContainingEntry_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(BattleTechResourceType resourceType, string id, ref List<VersionManifestMemoryStore> __result)
        {
            __result = BetterBTRL.Instance.GetMemoryStoresContainingEntry(resourceType, id);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.GetMemoryStoreByName))]
    public static class BattleTechResourceLocator_GetMemoryStoreByName_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(string name, ref VersionManifestMemoryStore __result)
        {
            __result = BetterBTRL.Instance.GetMemoryStoreByName(name);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.AllEntries))]
    public static class BattleTechResourceLocator_AllEntries_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(ref VersionManifestEntry[] __result)
        {
            __result = BetterBTRL.Instance.AllEntries();
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.AllEntriesOfResource))]
    public static class BattleTechResourceLocator_AllEntriesOfResource_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(BattleTechResourceType type, bool filterByOwnership, ref VersionManifestEntry[] __result)
        {
            __result = BetterBTRL.Instance.AllEntriesOfResource(type, filterByOwnership);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.AllEntriesOfResourceFromAddendum))]
    public static class BattleTechResourceLocator_AllEntriesOfResourceFromAddendum_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(BattleTechResourceType type, VersionManifestAddendum addendum, bool filterByOwnership, ref VersionManifestEntry[] __result)
        {
            __result = BetterBTRL.Instance.AllEntriesOfResourceFromAddendum(type, addendum, filterByOwnership);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.EntryByID))]
    public static class BattleTechResourceLocator_EntryByID_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(string id, BattleTechResourceType type, bool filterByOwnership, ref VersionManifestEntry __result)
        {
            __result = BetterBTRL.Instance.EntryByID(id, type, filterByOwnership);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.RemoveEntry))]
    public static class BattleTechResourceLocator_RemoveEntry_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(VersionManifestEntry entry)
        {
            BetterBTRL.Instance.RemoveEntry(entry);
            return false;
        }
    }
}
