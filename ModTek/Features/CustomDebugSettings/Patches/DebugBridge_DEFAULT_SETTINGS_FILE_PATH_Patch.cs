using System;
using BattleTech;

namespace ModTek.Features.CustomDebugSettings.Patches;

[HarmonyPatch(typeof(DebugBridge), nameof(DebugBridge.DEFAULT_SETTINGS_FILE_PATH), MethodType.Getter)]
internal static class DebugBridge_DEFAULT_SETTINGS_FILE_PATH_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static bool Prefix(ref string __result)
    {
        try
        {
            if (DebugSettingsFeature.TryGetSettingsPath("settings", out __result))
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log(e);
        }
        return true;
    }
}
