using System;
using HBS.Logging;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(Logger.LogImpl), nameof(Logger.LogImpl.Flush))]
internal static class LogImpl_Flush_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled && ModTek.Config.Logging.LogFlushToDisk;
    }

    [HarmonyPriority(Priority.High)]
    public static bool Prefix()
    {
        try
        {
            LoggingFeature.Flush();
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log("Couldn't rewrite Flush call", e);
        }
        return true;
    }
}