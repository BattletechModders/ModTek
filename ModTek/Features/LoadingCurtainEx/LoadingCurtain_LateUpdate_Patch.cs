using System;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.LoadingCurtainEx
{
    [HarmonyPatch(typeof(LoadingCurtain), "LateUpdate")]
    internal static class LoadingCurtain_LateUpdate_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled && ModTek.Config.ShowDataManagerStatsInLoadingCurtain;
        }

        public static void Postfix(
            LocalizableText ___popupLoadingText,
            LoadingSpinnerAndTip_Widget ___spinnerAndTipWidget
        ) {
            try
            {
                if (___popupLoadingText == null || ___spinnerAndTipWidget == null)
                {
                    return;
                }

                if (DataManagerLoadingCurtain.GetDataManagerStats(out var stats))
                {
                }

                if (stats == null)
                {
                    return;
                }

                var statsText = stats.GetStatsTextForCurtain();
                if (___popupLoadingText.IsActive())
                {
                    ___popupLoadingText.SetText(statsText);
                }

                if (___spinnerAndTipWidget.isActiveAndEnabled)
                {
                    var tipText = Traverse.Create(___spinnerAndTipWidget).Field("tipText").GetValue<LocalizableText>();
                    if (tipText != null)
                    {
                        tipText.SetText(statsText);
                    }
                }
            }
            catch (Exception e)
            {
                Log("Error running postfix", e);
            }
        }
    }
}
