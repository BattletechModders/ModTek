using BattleTech.UI;
using Harmony;

namespace ModTek.Features.Manifest.Patches
{
    // RequestResources
    [HarmonyPatch(typeof(SkirmishMechBayPanel), "LanceConfiguratorDataLoaded")]
    public static class SkirmishMechBayPanel_LanceConfiguratorDataLoaded_Patch
    {
        public static void Prefix()
        {
            ModsManifest.SimGameOrSkirmishLoaded();
        }
    }
}
