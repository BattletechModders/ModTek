using System.Linq;
using BattleTech.UI;
using Harmony;
using UnityEngine;
using Logger = HBS.Logging.Logger;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Features.LoadingCurtainEx
{
    /// <summary>
    /// Patch the logger to spit out errors to the loading screen curtain
    /// </summary>
    [HarmonyPatch(typeof(Logger), "HandleUnityLog")] // TODO integrate with logging feature?!
    internal static class Logger_HandleUnityLog_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(string logString, string stackTrace, LogType type)
        {
            if (!ModTek.HasLoaded
                || type != LogType.Error && type != LogType.Exception
                || ModTek.Config.UseErrorWhiteList && !ModTek.Config.ErrorWhitelist.Any(logString.StartsWith)
            ) {
                return;
            }

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
