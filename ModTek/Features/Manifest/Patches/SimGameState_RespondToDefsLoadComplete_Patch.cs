using BattleTech;
using Harmony;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(SimGameState), "RespondToDefsLoadComplete")]
    public static class SimGameState_RespondToDefsLoadComplete_Patch
    {
        public static void Prefix()
        {
            ModsManifest.SimGameOrSkirmishLoaded();
        }
    }
}