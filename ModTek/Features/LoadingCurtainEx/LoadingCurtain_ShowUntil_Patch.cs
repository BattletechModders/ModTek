using System;
using BattleTech.UI;
using ModTek.Features.LoadingCurtainEx.DataManagerStats;

namespace ModTek.Features.LoadingCurtainEx;

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
            Log.Main.Error?.Log("Failed running postfix", e);
        }
    }
}