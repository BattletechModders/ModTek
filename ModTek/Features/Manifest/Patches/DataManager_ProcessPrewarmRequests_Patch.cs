using System.Collections.Generic;
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

        internal static List<PrewarmRequest> PrewarmRequests = new();
        public static bool Prefix(IEnumerable<PrewarmRequest> toPrewarm)
        {
            if (toPrewarm != null)
            {
                PrewarmRequests.AddRange(toPrewarm);
            }
            return false;
        }
    }
}
