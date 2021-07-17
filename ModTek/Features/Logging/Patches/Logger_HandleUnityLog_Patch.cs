using Harmony;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch(typeof(HBS.Logging.Logger), nameof(HBS.Logging.Logger.HandleUnityLog), MethodType.Normal)]
    internal static class Logger_HandleUnityLog_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix()
        {
            return false;
        }
    }
}
