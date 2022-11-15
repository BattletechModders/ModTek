using Harmony;
using ModTek.Util;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(HBS.Logging.Logger), "CaptureUnityLogs")]
internal static class Logger_CaptureUnityLogs_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    private static readonly RunOnlyOnceHandler CleanupHandler = new();
    public static void Cleanup()
    {
        CleanupHandler.Run(UnityLogHandler.Setup);
    }

    public static bool Prefix()
    {
        return false;
    }
}