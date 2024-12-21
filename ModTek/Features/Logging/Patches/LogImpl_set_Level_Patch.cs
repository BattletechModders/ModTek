using System;
using HBS.Logging;
using ModTek.Common.Utils;

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
        var log = Log.Main.Trace;
        if (log == null)
        {
            return;
        }

        if (___level == __state)
        {
            return;
        }

        var debugText = ModTek.Config.Logging.DebugLogLevelSetters
            ? "\n" + DebugUtils.GetStackTraceWithoutPatch()
            : "";

        var oldLevel = __state?.Let(l => LogLevelExtension.LogToString(l) + $"({(int)__state})") ?? "null";
        var newLevel = ___level?.Let(l => LogLevelExtension.LogToString(l) + $"({(int)___level})") ?? "null";
        log.Log(
            $"Log Level of logger name={___name} changed from level={oldLevel} to level={newLevel}{debugText}"
        );
    }

    private static R Let<P, R>(this P s, Func<P, R> func) where P: notnull
    {
        return func(s);
    }
}