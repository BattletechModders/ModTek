using System;
using BattleTech.Data;
using Harmony;
using ModTek.Features.Manifest.BTRL;

namespace ModTek.Features.Manifest.Patches;

[HarmonyPatch(typeof(ContentPackIndex), nameof(ContentPackIndex.IsResourceOwned))]
internal static class ContentPackIndex_IsResourceOwned_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static bool Prefix(ContentPackIndex __instance, string resourceId, ref bool __result)
    {
        try
        {
            __result = BetterBTRL.Instance.PackIndex.IsResourceOwned(resourceId);
        }
        catch (Exception e)
        {
            Log.Main.Info?.Log("Error running prefix", e);
        }
        return false;
    }
}