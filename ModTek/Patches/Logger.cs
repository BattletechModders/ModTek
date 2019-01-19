using BattleTech.UI;
using Harmony;
using UnityEngine;

namespace ModTek
{
    /// <summary>
    /// Patch the logger to spit out errors to the loading screen curtain
    /// </summary>
    [HarmonyPatch(typeof(HBS.Logging.Logger), "HandleUnityLog")]
    public static class Logger_HandleUnityLog_Patch
    {
        public static void Postfix(string logString, string stackTrace, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception) && LoadingCurtain.IsVisible)
                LoadingCurtainErrorText.AddMessage(logString);
        }
    }
}
