using BattleTech.UI;
using Harmony;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch the logger to spit out errors to the loading screen curtain
    /// </summary>
    [HarmonyPatch(typeof(HBS.Logging.Logger), "HandleUnityLog")]
    public static class Logger_HandleUnityLog_Patch
    {
        public static void Postfix(string logString, string stackTrace, LogType type)
        {
            if (!ModTek.HasLoaded || type != LogType.Error && type != LogType.Exception
                || ModTek.Config.UseErrorWhiteList && !ModTek.Config.ErrorWhitelist.Exists(logString.StartsWith))
                return;

            if (LoadingCurtain.IsVisible && ModTek.Config.ShowLoadingScreenErrors)
            {
                LoadingCurtainErrorText.AddMessage(logString);
            }
            else if (!LoadingCurtain.IsVisible && ModTek.Config.ShowErrorPopup)
            {
                GenericPopupBuilder.Create("ModTek Detected Error", logString)
                    .AddButton("Continue")
                    .Render();
            }
        }
    }
}
