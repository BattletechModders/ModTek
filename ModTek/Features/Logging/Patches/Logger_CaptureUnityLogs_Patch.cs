using Harmony;
using UnityEngine;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch(typeof(HBS.Logging.Logger), "CaptureUnityLogs")]
    internal static class Logger_CaptureUnityLogs_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        private static bool IsSetup;
        public static void Cleanup()
        {
            if (ModTek.Enabled && !IsSetup)
            {
                UnityLogHandler.Setup();
                Application.logMessageReceived -= HBS.Logging.Logger.HandleUnityLog;
                IsSetup = true;
            }
        }

        public static bool Prefix()
        {
            return false;
        }
    }
}
