using System;
using System.IO;
using BattleTech;

namespace ModTek.Features.CustomDebugSettings.Patches;

[HarmonyPatch(typeof(DebugBridge), nameof(DebugBridge.GetCustomSettingsFilePath))]
internal static class DebugBridge_GetCustomSettingsFilePath_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static bool Prefix(string fileName, string defaultExtension, ref string __result)
    {
        try
        {
            if (DebugSettingsFeature.TryGetSettingsPath(Path.GetFileNameWithoutExtension(fileName), out __result))
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
