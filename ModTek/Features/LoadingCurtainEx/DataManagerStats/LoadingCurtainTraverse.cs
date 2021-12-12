using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using UnityEngine;

namespace ModTek.Features.LoadingCurtainEx.DataManagerStats
{
    internal class LoadingCurtainTraverse
    {
        private readonly LoadingCurtain instance;
        internal LoadingCurtainTraverse(LoadingCurtain instance)
        {
            this.instance = instance;
        }

        internal GameObject fullScreenContainer => Traverse.Create(instance).Field("fullScreenContainer").GetValue<GameObject>();
        internal GameObject popupContainer => Traverse.Create(instance).Field("popupContainer").GetValue<GameObject>();
        internal LocalizableText popupLoadingText => Traverse.Create(instance).Field("popupLoadingText").GetValue<LocalizableText>();
        internal LoadingSpinnerAndTip_Widget spinnerAndTipWidget => Traverse.Create(instance).Field("spinnerAndTipWidget").GetValue<LoadingSpinnerAndTip_Widget>();
    }
}
