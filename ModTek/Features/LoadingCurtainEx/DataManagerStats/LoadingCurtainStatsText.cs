using System.Diagnostics;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModTek.Features.LoadingCurtainEx.DataManagerStats;

internal static class LoadingCurtainStatsText
{
    internal static void ShowUntil(LoadingCurtain loadingCurtain)
    {
        SetText(loadingCurtain, "");
    }

    private static readonly Stopwatch sw;
    static LoadingCurtainStatsText()
    {
        sw = new Stopwatch();
        sw.Start();
    }

    internal static void LateUpdate(LoadingCurtain loadingCurtain)
    {
        if (!LoadingCurtain.IsVisible)
        {
            return;
        }

        if (sw.ElapsedMilliseconds < ModTek.Config.DataManagerUnfreezeDelta * 1000)
        {
            return;
        }
        sw.Restart();

        DataManagerStats.GetStats(out var stats);
        if (stats != null)
        {
            var statsText = stats.GetStatsTextForCurtain();
            SetText(loadingCurtain, statsText);
        }
    }

    internal static void SetText(LoadingCurtain loadingCurtain, string text)
    {
        if (loadingCurtain == null)
        {
            return;
        }
        var popupContainer = loadingCurtain.popupContainer;
        if (popupContainer == null)
        {
            return;
        }
        if (popupContainer.activeInHierarchy)
        {
            SetPopupExtraText(popupContainer, text);
        }
        else
        {
            SetFullScreenText(loadingCurtain.spinnerAndTipWidget, text);
        }
    }

    internal static void Init(LoadingCurtain loadingCurtain)
    {
        SetupPopupStatsGameObject(loadingCurtain.popupLoadingText);
        SetupFullScreenStatsGameObject(loadingCurtain.spinnerAndTipWidget);
    }

    private const string PopupGameObjectName = "ModTek_PopupStats";
    private static void SetPopupExtraText(GameObject container, string text)
    {
        var transform = container.transform.Find("Representation/loadElementLayout/" + PopupGameObjectName);
        if (transform == null)
        {
            return;
        }
        transform.GetComponent<LocalizableText>().SetText(text);
        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
    }
    private static void SetupPopupStatsGameObject(LocalizableText popupLoadingText)
    {
        var template = popupLoadingText.gameObject;
        var go = Object.Instantiate(template, null);
        go.name = PopupGameObjectName;

        {
            var component = go.AddComponent<LayoutElement>();
            component.ignoreLayout = true; // make sure to stay outside of layout
            component.enabled = true;
        }

        {
            var component = go.GetComponent<LocalizableText>();
            component.enableAutoSizing = false;
            component.fontSize = 14;
            component.autoSizeTextContainer = false; // does not work
            component.alignment = TextAlignmentOptions.BottomLeft;
            component.SetText("");
        }

        {
            var component = go.GetComponent<RectTransform>();
            component.sizeDelta = new Vector2(100, 20);
            component.offsetMin = new Vector2(0, 0);
            component.offsetMax = new Vector2(100, 20);
            component.pivot = new Vector2(0, 0);
            component.anchorMin = new Vector2(0, 1);
            component.anchorMax = new Vector2(0, 1);
            component.anchoredPosition = new Vector2(0, 0);
        }

        go.SetActive(true);
        go.transform.SetParent(popupLoadingText.transform.parent);
    }

    private const string FullScreenGameObjectName = "ModTek_FullScreenStats";
    private static void SetFullScreenText(LoadingSpinnerAndTip_Widget spinnerAndTipWidget, string text)
    {
        var transform = spinnerAndTipWidget.transform.Find(FullScreenGameObjectName);
        if (transform == null)
        {
            return;
        }
        transform.GetComponent<LocalizableText>().SetText(text);
        transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
    }
    private static void SetupFullScreenStatsGameObject(LoadingSpinnerAndTip_Widget widget)
    {
        var template = widget.transform.Find("message_text").gameObject;
        var go = Object.Instantiate(template, null);
        go.name = FullScreenGameObjectName;

        {
            var component = go.AddComponent<LayoutElement>();
            component.ignoreLayout = true; // make sure to stay outside of layout
            component.enabled = true;
        }

        {
            var component = go.GetComponent<LocalizableText>();
            component.enableAutoSizing = false;
            component.fontSize = 14;
            component.autoSizeTextContainer = false; // does not work
            component.alignment = TextAlignmentOptions.TopLeft;
            component.SetText("");
        }

        {
            var component = go.GetComponent<RectTransform>();
            component.sizeDelta = new Vector2(150, 20);
            component.offsetMin = new Vector2(0, 0);
            component.offsetMax = new Vector2(150, 20);
            component.pivot = new Vector2(0, 1);
            component.anchorMin = new Vector2(0, 0);
            component.anchorMax = new Vector2(0, 0);
            component.anchoredPosition = new Vector2(0, 0);
        }

        go.SetActive(true);
        go.transform.SetParent(template.transform.parent);
    }
}