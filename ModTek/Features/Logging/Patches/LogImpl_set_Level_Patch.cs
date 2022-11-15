using System;
using Harmony;
using HBS.Logging;
using ModTek.Util;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(Logger.LogImpl), nameof(Logger.LogImpl.Level), MethodType.Setter)]
internal static class LogImpl_set_Level_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    [HarmonyPriority(Priority.High)]
    public static void Prefix(LogLevel? ___level, out LogLevel? __state)
    {
        __state = ___level;
    }

    [HarmonyPriority(Priority.Low)]
    public static void Postfix(string ___name, LogLevel? ___level, LogLevel? __state)
    {
        if (___level != __state)
        {
            var debugText = ModTek.Config.Logging.DebugLogLevelSetters
                ? "\n" + DebugUtils.GetStackTraceWithoutPatch()
                : "";

            var oldLevel = __state?.Let(l => LogLevelExtension.LogToString(l) + $"({(int)__state})") ?? "null";
            var newLevel = ___level?.Let(l => LogLevelExtension.LogToString(l) + $"({(int)___level})") ?? "null";
            Log.Main.Trace?.Log(
                $"Log Level of logger name={___name} changed from level={oldLevel} to level={newLevel}{debugText}"
            );
        }
    }

    private static R Let<P, R>(this P s, Func<P, R> func) where P: notnull
    {
        return func(s);
    }
}