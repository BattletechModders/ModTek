using System.Reflection;
using Harmony;
using HBS.Logging;
using ModTek.Util;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch]
    internal static class LogImpl_set_Level_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled && false;
        }

        public static MethodInfo TargetMethod()
        {
            var logImpl = AccessTools.Inner(typeof(Logger), "LogImpl");
            var original = AccessTools.Property(logImpl, "Level").SetMethod;
            return original;
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
                MTLogger.Debug.Log(
                    $"Log Level changed, logger name={___name} (old)level={__state} (new)level={___level}\n"
                    + DebugUtils.GetStackTraceWithoutPatch()
                );
            }
        }
    }
}
