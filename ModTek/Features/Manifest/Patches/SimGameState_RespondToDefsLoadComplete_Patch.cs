using System;
using BattleTech;
using Harmony;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.Patches
{
    // RequestDataManagerResources
    [HarmonyPatch(typeof(SimGameState), "RespondToDefsLoadComplete")]
    public static class SimGameState_RespondToDefsLoadComplete_Patch
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