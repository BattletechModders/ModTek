using System;
using BattleTech.UI;
using Harmony;
using ModTek.Features.LoadingCurtainEx.DataManagerStats;
using ModTek.Features.Logging;

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
                MTLogger.Error.Log("Failed running postfix", e);;
            }
        }
    }
}
