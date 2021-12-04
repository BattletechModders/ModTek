using System;
using BattleTech.UI;
using Harmony;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.Patches
{
    // LoadLanceConfiguratorData
    [HarmonyPatch(typeof(SkirmishSettings_Beta), "OnLoadComplete")]
    public class SkirmishSettings_Beta_OnLoadComplete_Patch
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