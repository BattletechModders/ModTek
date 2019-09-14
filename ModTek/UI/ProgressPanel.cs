using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Harmony;
using UnityEngine;
using UnityEngine.UI;
using static ModTek.Util.Logger;

// ReSharper disable UnusedMember.Local

namespace ModTek.UI
{
    internal struct ProgressReport
    {
        public float Progress { get; }
        public string SliderText { get; }
        public string LoadingText { get; }
        public bool ForceFrame { get; }

        public ProgressReport(float progress, string sliderText, string loadingText, bool forceFrame = false)
        {
            Progress = progress;
            SliderText = sliderText;
            LoadingText = loadingText;
            ForceFrame = forceFrame;
        }
    }

    internal static class ProgressPanel
    {
        private const string ASSET_BUNDLE_NAME = "modtekassetbundle";
        private static ProgressBarLoadingBehavior loadingBehavior;

        public class ProgressBarLoadingBehavior : MonoBehaviour
        {
            private const int FRAME_TIME = 50; // around 20fps

            public Text SliderText { get; set; }
            public Text LoadingText { get; set; }
            public Slider Slider { get; set; }
            public Action FinishAction { get; set; }

            private LinkedList<Func<IEnumerator<ProgressReport>>> WorkList = new LinkedList<Func<IEnumerator<ProgressReport>>>();

            private void Start()
            {
                StartCoroutine(RunWorkList());
            }

            public void AddWork(Func<IEnumerator<ProgressReport>> work)
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
                    bool didWork;

                    do
                    {
                        try
                        {
                            didWork = workEnumerator.MoveNext();
                        }
                        catch (Exception e)
                        {
                            LogException("\nUncaught ModTek exception!", e);

                            Slider.value = 1.0f;
                            SliderText.text = "ModTek Died!";
                            LoadingText.text = $"See \"{ModTek.GetRelativePath(LogPath, ModTek.GameDirectory)}\"";

                            ModTek.Finish();

                            yield break;
                        }

                        var report = workEnumerator.Current;

                        if (sw.ElapsedMilliseconds <= FRAME_TIME && !report.ForceFrame)
                            continue;

                        Slider.value = report.Progress;
                        SliderText.text = report.SliderText;
                        LoadingText.text = report.LoadingText;

                        sw.Reset();
                        sw.Start();
                        yield return null;
                    }
                    while (didWork);

                    yield return null;
                }

                Slider.value = 1.0f;
                SliderText.text = "Game now loading";
                LoadingText.text = "";
                yield return null;

                FinishAction.Invoke();
            }
        }

        internal static bool Initialize(string assetDirectory, string panelTitle)
        {
            var assetBundle = AssetBundle.LoadFromFile(Path.Combine(assetDirectory, ASSET_BUNDLE_NAME));
            if (assetBundle == null)
            {
                Log($"Error loading asset bundle {ASSET_BUNDLE_NAME}");
                return false;
            }

            var canvasPrefab = assetBundle.LoadAsset<GameObject>("ProgressBar_Canvas");
            var canvasGameObject = UnityEngine.Object.Instantiate(canvasPrefab);
            var panelTitleText = GameObject.Find("ProgressBar_Title")?.GetComponent<Text>();
            var sliderText = GameObject.Find("ProgressBar_Slider_Text")?.GetComponent<Text>();
            var loadingText = GameObject.Find("ProgressBar_Loading_Text")?.GetComponent<Text>();
            var sliderGameObject = GameObject.Find("ProgressBar_Slider");

            if (panelTitleText == null || sliderText == null || loadingText == null || sliderGameObject == null)
            {
                Log("Error loading a GameObject from asset bundle");
                return false;
            }

            var slider = sliderGameObject.GetComponent<Slider>();
            panelTitleText.text = panelTitle;

            loadingBehavior = sliderGameObject.AddComponent<ProgressBarLoadingBehavior>();
            loadingBehavior.SliderText = sliderText;
            loadingBehavior.LoadingText = loadingText;
            loadingBehavior.Slider = slider;
            loadingBehavior.FinishAction = () =>
            {
                assetBundle.Unload(true);
                UnityEngine.Object.Destroy(canvasGameObject);
                TriggerGameLoading();
            };

            return true;
        }

        internal static void SubmitWork(Func<IEnumerator<ProgressReport>> workFunc)
        {
            loadingBehavior.AddWork(workFunc);
        }

        private static void TriggerGameLoading()
        {
            // Reactivate the main menu loading by calling the attached ActivateAndClose behavior on the UnityGameInstance (initializes a handful of different things);
            var activateAfterInit = GameObject.Find("Main").GetComponent<ActivateAfterInit>();
            Traverse.Create(activateAfterInit).Method("ActivateAndClose").GetValue();
        }
    }
}
