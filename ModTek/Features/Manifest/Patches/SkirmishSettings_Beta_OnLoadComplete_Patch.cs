using BattleTech.UI;
using Harmony;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(SkirmishSettings_Beta), "OnLoadComplete")]
    public class SkirmishSettings_Beta_OnLoadComplete_Patch
    {
        public static void Prefix()
        {
            ModsManifest.SimGameOrSkirmishLoaded();
        }
    }
}