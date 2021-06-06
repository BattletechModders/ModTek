using Harmony;
using UnityEngine;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch(typeof(Debug), "logger", MethodType.Getter)]
    internal static class DebugLoggerAttacher
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        private static ILogger DebugLog;
        public static bool Prefix(ref ILogger __result)
        {
            DebugLog ??= new UnityEngine.Logger(new LoggerProxy(HBS.Logging.Logger.GetLogger("UnityEngine.Debug")));
            __result = DebugLog;
            return false;
        }
    }
}
