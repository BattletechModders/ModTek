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
    public static bool Prefix(Logger.LogImpl __instance, LogLevel level, ref bool __result)
    {
        __result = LogLevelExtension.IsLogLevelEnabled(__instance.EffectiveLevel, level);
        return false;
    }
}