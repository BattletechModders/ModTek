using System;
using BattleTech.UI;
using Harmony;

namespace ModTek.Features.Profiler.Patches;

[HarmonyPatch(typeof(UIManager), nameof(UIManager.LateUpdate))]
internal static class UIManager_LateUpdate_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled && ModTek.Config.ProfilerEnabled;
    }

    public static void Postfix(LoadingCurtain __instance)
    {
        try
        {
            ProfilerStats.LogIfChanged();
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log("Failed running postfix", e);
        }
    }
}