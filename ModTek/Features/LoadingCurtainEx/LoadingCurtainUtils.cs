using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;

namespace ModTek.Features.LoadingCurtainEx
{
    internal static class LoadingCurtainUtils
    {
        internal static void SetActivePopupText(string text)
        {
            GetActivePopupText()?.SetText(text);
        }

        private static LocalizableText GetActivePopupText()
        {
            var lc = GetActive();
            if (lc == null)
            {
                return null;
            }
            return Traverse
                .Create(lc)
                .Field("popupLoadingText")
                .GetValue<LocalizableText>();
        }

        internal static LoadingCurtain GetActive()
        {
            return Traverse
                .Create<LoadingCurtain>()
                .Field("activeInstance")
                .GetValue<LoadingCurtain>();
        }
    }
}
