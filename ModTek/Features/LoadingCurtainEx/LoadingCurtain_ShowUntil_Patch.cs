using System;
using BattleTech.UI;
using Harmony;
using ModTek.Features.LoadingCurtainEx.DataManagerStats;
using ModTek.Features.Logging;

namespace ModTek.Features.LoadingCurtainEx
{
    [HarmonyPatch(typeof(LoadingCurtain), nameof(LoadingCurtain.ShowUntil))]
    internal static class LoadingCurtain_ShowUntil_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled && ModTek.Config.ShowDataManagerStatsInLoadingCurtain;
        }

        public static void Postfix(LoadingCurtain __instance)
        {
            try
            {
                LoadingCurtainStatsText.ShowUntil(__instance);
            }
            catch (Exception e)
            {
                MTLogger.Error.Log("Failed running postfix", e);
            }
        }
    }
}
