using System;
using BattleTech.UI;
using Harmony;
using ModTek.Features.LoadingCurtainEx.DataManagerStats;

namespace ModTek.Features.LoadingCurtainEx;

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
            Log.Main.Error?.Log("Failed running postfix", e);;
        }
    }
}