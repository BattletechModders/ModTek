using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ModTek
{
    internal struct ProgressReport
    {
        public float Progress { get; set; }
        public string SliderText { get; set; }
        public string LoadingText { get; set; }

        public ProgressReport(float progress, string sliderText, string loadingText)
        {
            Progress = progress;
            SliderText = sliderText;
            LoadingText = loadingText;
        }
    }

    internal static class ProgressPanel
    {
        public const string ASSET_BUNDLE_NAME = "modtekassetbundle";
        private static ProgressBarLoadingBehavior LoadingBehavior;

        public class ProgressBarLoadingBehavior : MonoBehaviour
        {
            private static readonly int FRAME_TIME = 50; // around 20fps

            public Text SliderText { get; set; }
            public Text LoadingText { get; set; }
            public Slider Slider { get; set; }
            public Action FinishAction { get; set; }

            private LinkedList<Func<IEnumerator<ProgressReport>>> WorkList = new LinkedList<Func<IEnumerator<ProgressReport>>>();

            void Start()
            {
                StartCoroutine(RunWorkList());
            }

            public void SubmitWork(Func<IEnumerator<ProgressReport>> work)
            {
                WorkList.AddLast(work);
            }

            private IEnumerator RunWorkList()
            {
                var sw = new Stopwatch();
                sw.Start();
                foreach (var workFunc in WorkList)
                {
                    var workEnumerator = workFunc.Invoke();
                    while (workEnumerator.MoveNext())
                    {
                        if (sw.ElapsedMilliseconds > FRAME_TIME)
                        {
                            var report = workEnumerator.Current;
                            Slider.value = report.Progress;
                            SliderText.text = report.SliderText;
                            LoadingText.text = report.LoadingText;

                            sw.Reset();
                            sw.Start();
                            yield return null;
                        }
                    }
                    yield return null;
                }

                Slider.value = 1.0f;
                SliderText.text = "Game now loading";
                LoadingText.text = "";
                yield return null;

                // TODO: why was this here
                // Let Finished stay on the screen for a moment
                // Thread.Sleep(1000);

                FinishAction.Invoke();
                yield break;
            }
        }

        internal static bool Initialize(string assetDirectory, string panelTitle)
        {
            var assetBundle = AssetBundle.LoadFromFile(Path.Combine(assetDirectory, ASSET_BUNDLE_NAME));
            if (assetBundle == null)
            {
                string message = $"Error loading asset bundle {ASSET_BUNDLE_NAME}";
                return false;
            }

            var canvasPrefab = assetBundle.LoadAsset<GameObject>("ProgressBar_Canvas");
            var canvasGameObject = GameObject.Instantiate(canvasPrefab);

            var panelTitleText = GameObject.Find("ProgressBar_Title")?.GetComponent<Text>();
            if (panelTitleText == null)
            {
                Logger.LogWithDate("Error loading ProgressBar_Title");
                return false;
            }

            var sliderText = GameObject.Find("ProgressBar_Slider_Text")?.GetComponent<Text>();
            if (sliderText == null)
            {
                Logger.LogWithDate("Error loading ProgressBar_Slider_Text");
                return false;
            }

            var loadingText = GameObject.Find("ProgressBar_Loading_Text")?.GetComponent<Text>();
            if (loadingText == null)
            {
                Logger.LogWithDate("Error loading ProgressBar_Loading_Text");
                return false;
            }

            var sliderGameObject = GameObject.Find("ProgressBar_Slider");
            var slider = sliderGameObject?.GetComponent<Slider>();
            if (sliderGameObject == null)
            {
                Logger.LogWithDate("Error loading ProgressBar_Slider");
                return false;
            }

            panelTitleText.text = panelTitle;

            LoadingBehavior = sliderGameObject.AddComponent<ProgressBarLoadingBehavior>();
            LoadingBehavior.SliderText = sliderText;
            LoadingBehavior.LoadingText = loadingText;
            LoadingBehavior.Slider = slider;
            LoadingBehavior.FinishAction = () =>
            {
                assetBundle.Unload(true);
                GameObject.Destroy(canvasGameObject);
                TriggerGameLoading();
            };

            return true;
        }

        internal static void SubmitWork(Func<IEnumerator<ProgressReport>> workFunc)
        {
            LoadingBehavior.SubmitWork(workFunc);
        }

        private static void TriggerGameLoading()
        {
            // Reactivate the main menu loading by calling the attached ActivateAndClose behavior on the UnityGameInstance (initializes a handful of different things);
            var activateAfterInit = GameObject.Find("Main").GetComponent<ActivateAfterInit>();
            Traverse.Create(activateAfterInit).Method("ActivateAndClose").GetValue();
        }
    }
}
