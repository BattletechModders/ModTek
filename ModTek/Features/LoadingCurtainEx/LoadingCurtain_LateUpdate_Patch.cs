using System;
using BattleTech.UI;
using Harmony;
using ModTek.Features.LoadingCurtainEx.DataManagerStats;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.LoadingCurtainEx
{
    [HarmonyPatch(typeof(LoadingCurtain), "LateUpdate")]
    internal static class LoadingCurtain_LateUpdate_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled && ModTek.Config.ShowDataManagerStatsInLoadingCurtain;
        }

        public static void Postfix(LoadingCurtain __instance)
        {
            try
            {
                LoadingCurtainStatsText.LateUpdate(__instance);
            }
            catch (Exception e)
            {
                Log("Error running postfix", e);
            }
        }
    }
}
