using BattleTech.Data;
using Harmony;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(DataManager), nameof(DataManager.ProcessPrewarmRequests))]
    public static class DataManager_ProcessPrewarmRequests_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled && ModTek.Config.DelayPrewarmUntilPreload;
        }

        public static bool Prefix()
        {
            ModsManifestPreloader.isPrewarmRequestedForNextPreload = true;
            return false;
        }
    }
}
