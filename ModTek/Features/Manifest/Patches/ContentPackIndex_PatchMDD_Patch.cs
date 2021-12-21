using System;
using BattleTech.Data;
using Harmony;
using ModTek.Features.Manifest.BTRL;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(ContentPackIndex), "PatchMDD")]
    public static class ContentPackIndex_PatchMDD_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(ContentPackIndex __instance)
        {
            try
            {
                BetterBTRL.Instance.PackIndex.PatchMDD(__instance);
            }
            catch (Exception e)
            {
                Log("Error running postfix", e);
            }
        }
    }
}
