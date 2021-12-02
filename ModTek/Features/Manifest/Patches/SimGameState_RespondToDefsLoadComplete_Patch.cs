using BattleTech;
using Harmony;

namespace ModTek.Features.Manifest.Patches
{
    // RequestDataManagerResources
    [HarmonyPatch(typeof(SimGameState), "RespondToDefsLoadComplete")]
    public static class SimGameState_RespondToDefsLoadComplete_Patch
    {
        public static void Prefix()
        {
            ModsManifest.SaveCaches();
        }
    }
}