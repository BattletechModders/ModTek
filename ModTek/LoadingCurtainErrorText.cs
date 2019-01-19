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

        public static void Clear()
        {
            if (tmProText == null)
                return;

            tmProText.text = "";
            Message = "[ModTek] Detected errors (might not be important!) -- For more info check ";

            var cleanLogPath = Path.Combine(ModTek.ModsDirectory, "cleaned_output_log.txt");
            if (File.Exists(cleanLogPath))
                Message += $"\"{ModTek.GetRelativePath(cleanLogPath, ModTek.GameDirectory)}\"\n";
            else
                Message += $"\"output_log.txt\"\n";
        }

        public static void Setup(GameObject parent)
        {
            if (parent == null || parentLoadingCurtain == parent)
                return;

            parentLoadingCurtain = parent;

            textGameObject = new GameObject("ModTek_LoadingCurtainErrorText");
            var rectTransform = textGameObject.AddComponent<RectTransform>();
            textGameObject.transform.SetParent(parentLoadingCurtain.transform);

            rectTransform.sizeDelta = new Vector2(1980, 250);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, -200);

            tmProText = textGameObject.AddComponent<TextMeshProUGUI>();
            tmProText.enableWordWrapping = true;
            tmProText.alignment = TextAlignmentOptions.Top;
            tmProText.fontSize = 18;

            Clear();
        }
    }
}
