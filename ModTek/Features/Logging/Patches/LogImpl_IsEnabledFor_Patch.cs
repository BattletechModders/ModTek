using Harmony;
using HBS.Logging;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(Logger.LogImpl), nameof(Logger.LogImpl.IsEnabledFor))]
internal static class LogImpl_IsEnabledFor_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    [HarmonyPriority(Priority.High)]
    public static bool Prefix(LogLevel level, ref bool __result)
    {
        __result = LogLevelExtension.IsLogLevelEnabled(level);
        return false;
    }
}