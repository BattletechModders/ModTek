using Harmony;
using UnityEngine;
using Logger = HBS.Logging.Logger;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(Logger), nameof(Logger.CaptureUnityLogs))]
internal static class Logger_CaptureUnityLogs_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static bool Prefix()
    {
        Application.logMessageReceived -= Logger.HandleUnityLog;
        return false;
    }
}