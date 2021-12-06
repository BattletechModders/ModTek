using System;
using Harmony;
using UnityEngine;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch(typeof(Debug), "logger", MethodType.Getter)]
    internal static class Debug_Logger_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        private static ILogger DebugLog;
        public static bool Prefix(ref ILogger __result)
        {
            try
            {
                DebugLog = DebugLog ?? new Logger(new UnityLogHandlerAdapter(HBS.Logging.Logger.GetLogger("UnityEngine.Debug")));
                __result = DebugLog;
            }
            catch (Exception e)
            {
                Log("Couldn't inject unity debug logger adapter",  e);
            }
            return false;
        }
    }
}
