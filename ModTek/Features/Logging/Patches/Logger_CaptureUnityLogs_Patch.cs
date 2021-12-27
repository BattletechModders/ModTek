using Harmony;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch(typeof(HBS.Logging.Logger), "CaptureUnityLogs")]
    internal static class Logger_CaptureUnityLogs_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Cleanup()
        {
            if (ModTek.Enabled)
            {
                UnityLogHandler.Setup();
            }
        }

        public static bool Prefix()
        {
            return false;
        }
    }
}
