using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using Harmony;
using ModTek.Features.Logging;
using ModTek.Features.Manifest.BTRL;

// ReSharper disable RedundantAssignment

namespace ModTek.Features.Manifest.Patches
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
            try
            {
                BetterBTRL.Instance.SetContentPackIndex(contentPackIndex);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                BetterBTRL.Instance.ApplyAddendum(addendum);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                BetterBTRL.Instance.RemoveAddendum(addendum);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                __result = BetterBTRL.Instance.GetAddendumByName(name);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                BetterBTRL.Instance.ApplyMemoryStore(memoryStore);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                BetterBTRL.Instance.RemoveMemoryStore(memoryStore);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                __result = BetterBTRL.Instance.GetMemoryStoresContainingEntry(resourceType, id);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                __result = BetterBTRL.Instance.GetMemoryStoreByName(name);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                __result = BetterBTRL.Instance.AllEntries();
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                __result = BetterBTRL.Instance.AllEntriesOfResource(type, filterByOwnership);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                __result = BetterBTRL.Instance.AllEntriesOfResourceFromAddendum(type, addendum, filterByOwnership);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                __result = BetterBTRL.Instance.EntryByID(id, type, filterByOwnership);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
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
            try
            {
                BetterBTRL.Instance.RemoveEntry(entry);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
            return false;
        }
    }
}
