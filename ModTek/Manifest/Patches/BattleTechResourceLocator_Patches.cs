using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using Harmony;
using JetBrains.Annotations;
using ModTek.Manifest.BTRL;

// ReSharper disable RedundantAssignment

namespace ModTek.Manifest.Patches
{
    // fix constructor being called
    [HarmonyPatch(typeof(BattleTechResourceLocator), "RefreshTypedEntries")]
    public static class BattleTechResourceLocator_RefreshTypedEntries_Patch
    {
        [UsedImplicitly]
        public static bool Prefix()
        {
            return false;
        }
    }

    // redirect all public methods to new class

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.Dispose))]
    public static class BattleTechResourceLocator_Dispose_Patch
    {
        [UsedImplicitly]
        public static bool Prefix()
        {
            BTRLInstance.Locator.Dispose();
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.SetContentPackIndex))]
    public static class BattleTechResourceLocator_SetContentPackIndex_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(ContentPackIndex contentPackIndex)
        {
            BTRLInstance.Locator.SetContentPackIndex(contentPackIndex);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.ApplyAddendum))]
    public static class BattleTechResourceLocator_ApplyAddendum_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(VersionManifestAddendum addendum)
        {
            BTRLInstance.Locator.ApplyAddendum(addendum);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.RemoveAddendum))]
    public static class BattleTechResourceLocator_RemoveAddendum_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(VersionManifestAddendum addendum)
        {
            BTRLInstance.Locator.RemoveAddendum(addendum);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.GetAddendumByName))]
    public static class BattleTechResourceLocator_GetAddendumByName_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(string name, ref VersionManifestAddendum __result)
        {
            __result = BTRLInstance.Locator.GetAddendumByName(name);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.ApplyMemoryStore))]
    public static class BattleTechResourceLocator_ApplyMemoryStore_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(VersionManifestMemoryStore memoryStore)
        {
            BTRLInstance.Locator.ApplyMemoryStore(memoryStore);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.RemoveMemoryStore))]
    public static class BattleTechResourceLocator_RemoveMemoryStore_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(VersionManifestMemoryStore memoryStore)
        {
            BTRLInstance.Locator.RemoveMemoryStore(memoryStore);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.GetMemoryStoresContainingEntry))]
    public static class BattleTechResourceLocator_GetMemoryStoresContainingEntry_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(BattleTechResourceType resourceType, string id, ref List<VersionManifestMemoryStore> __result)
        {
            __result = BTRLInstance.Locator.GetMemoryStoresContainingEntry(resourceType, id);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.GetMemoryStoreByName))]
    public static class BattleTechResourceLocator_GetMemoryStoreByName_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(string name, ref VersionManifestMemoryStore __result)
        {
            __result = BTRLInstance.Locator.GetMemoryStoreByName(name);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.AllEntries))]
    public static class BattleTechResourceLocator_AllEntries_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(ref VersionManifestEntry[] __result)
        {
            __result = BTRLInstance.Locator.AllEntries();
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.AllEntriesOfResource))]
    public static class BattleTechResourceLocator_AllEntriesOfResource_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(BattleTechResourceType type, bool filterByOwnership, ref VersionManifestEntry[] __result)
        {
            __result = BTRLInstance.Locator.AllEntriesOfResource(type, filterByOwnership);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.AllEntriesOfResourceFromAddendum))]
    public static class BattleTechResourceLocator_AllEntriesOfResourceFromAddendum_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(BattleTechResourceType type, VersionManifestAddendum addendum, bool filterByOwnership, ref VersionManifestEntry[] __result)
        {
            __result = BTRLInstance.Locator.AllEntriesOfResourceFromAddendum(type, addendum, filterByOwnership);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.EntryByID))]
    public static class BattleTechResourceLocator_EntryByID_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(string id, BattleTechResourceType type, bool filterByOwnership, ref VersionManifestEntry __result)
        {
            __result = BTRLInstance.Locator.EntryByID(id, type, filterByOwnership);
            return false;
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), nameof(BattleTechResourceLocator.RemoveEntry))]
    public static class BattleTechResourceLocator_RemoveEntry_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(VersionManifestEntry entry)
        {
            BTRLInstance.Locator.RemoveEntry(entry);
            return false;
        }
    }
}
