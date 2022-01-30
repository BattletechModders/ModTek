using System.Threading;
using Harmony;
using HBS.Logging;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch(typeof(Thread), "StartInternal")]
    internal static class Thread_StartInternal_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled && ModTek.Config.Logging.LogThreadStarts;
        }

        public static void Postfix(Thread __instance)
        {
            var st = new System.Diagnostics.StackTrace(4).ToString();
            LoggingFeature.LogAtLevel(
                "Debugger",
                LogLevel.Debug,
                "A thread was started with id " + __instance.ManagedThreadId + st,
                null,
                null
            );
        }
    }
}
