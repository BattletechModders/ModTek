using System;
using BattleTech.Data;
using Harmony;
using ModTek.Features.Manifest.BTRL;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(ContentPackIndex), nameof(ContentPackIndex.IsResourceOwned))]
    public static class ContentPackIndex_IsResourceOwned_Patch
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
                Log("Error running prefix", e);
            }
            return false;
        }
    }
}
