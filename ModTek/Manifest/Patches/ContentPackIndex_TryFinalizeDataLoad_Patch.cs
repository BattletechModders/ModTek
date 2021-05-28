using BattleTech.Data;
using Harmony;
using ModTek.Manifest.BTRL;

namespace ModTek.Manifest.Patches
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
            BetterBTRL.Instance.TryFinalizeDataLoad(__instance);
        }
    }
}
