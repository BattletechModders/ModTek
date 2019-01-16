using BattleTech;
using HBS.Logging;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace ModTek
{
    public static class LoadingCurtainErrorText
    {
        public static GameObject textGameObject;
        public static TextMeshProUGUI tmProText;

        private static string Message;
        private static GameObject parentLoadingCurtain;

        public static void AddMessage(string message)
        {
            Message += message + "\n";
            tmProText.text = Message;
        }

        public static void Setup(GameObject parent)
        {
            var cleanLogPath = Path.Combine(ModTek.ModsDirectory, "cleaned_output_log.txt");
            if (File.Exists(cleanLogPath))
                Message = $"[ModTek] For more info check \"{ModTek.GetRelativePath(cleanLogPath, ModTek.GameDirectory)}\"\n";
            else
                Message = $"[ModTek] For more info check \"output_log.txt\"\n";

            if (parent == null || parentLoadingCurtain == parent)
                return;

            parentLoadingCurtain = parent;

            textGameObject = new GameObject();
            var rectTransform = textGameObject.AddComponent<RectTransform>();
            textGameObject.transform.SetParent(parentLoadingCurtain.transform);

            rectTransform.sizeDelta = new Vector2(1000, 250);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, -200);

            tmProText = textGameObject.AddComponent<TextMeshProUGUI>();
            tmProText.enableWordWrapping = true;
            tmProText.alignment = TextAlignmentOptions.Top;
            tmProText.extraPadding = true;
            tmProText.fontSize = 20;
        }
    }
}
