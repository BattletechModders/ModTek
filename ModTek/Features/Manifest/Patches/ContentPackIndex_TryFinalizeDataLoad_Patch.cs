using System;
using BattleTech.Data;
using Harmony;
using ModTek.Features.Manifest.BTRL;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(ContentPackIndex), "TryFinalizeDataLoad")]
    public static class ContentPackIndex_TryFinalizeDataLoad_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(ContentPackIndex __instance)
        {
            try
            {
                BetterBTRL.Instance.TryFinalizeDataLoad(__instance);
            }
            catch (Exception e)
            {
                Log("Error finalizing content pack index loading", e);
            }
        }
    }
}
