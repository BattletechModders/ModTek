using System;
using BattleTech.UI;
using Harmony;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.Patches
{
    // RequestResources
    [HarmonyPatch(typeof(SkirmishMechBayPanel), "LanceConfiguratorDataLoaded")]
    public static class SkirmishMechBayPanel_LanceConfiguratorDataLoaded_Patch
    {
        public static void Prefix()
        {
            try
            {
                ModsManifest.SaveCaches();
            }
            catch (Exception e)
            {
                Log("Error running prefix", e);
            }
        }
    }
}
