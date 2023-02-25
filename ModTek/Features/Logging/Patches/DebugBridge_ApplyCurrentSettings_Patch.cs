using System;
using BattleTech;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(DebugBridge), nameof(DebugBridge.ApplyCurrentSettings))]
internal static class DebugBridge_ApplyCurrentSettings_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Prefix()
    {
        try
        {
            var loggerLevels = DebugBridge.settings.loggerLevels;
            foreach (var overrideLoggerLevel in ModTek.Config.Logging.OverrideLoggerLevels)
            {
                loggerLevels[overrideLoggerLevel.Key] = overrideLoggerLevel.Value;
            }
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log(e);
        }
    }
}