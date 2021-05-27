using BattleTech.Data;
using Harmony;
using ModTek.Manifest.BTRL;

namespace ModTek.Manifest.Patches
{
    [HarmonyPatch(typeof(ContentPackIndex), "PatchMDD")]
    internal static class ContentPackIndex_PatchMDD_Patch
    {
        internal static void Postfix()
        {
            BetterBTRL.Instance.PatchMDD();
        }
    }
}
