using BattleTech;
using Harmony;
using HBS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace ModTek
{
    class ProgressReport
    {
        public float Progress { get; set; }
        public string SliderText { get; set; }
        public string LoadingText { get; set; }

        public ProgressReport(float progress, string sliderText, string loadingText)
        {
            this.Progress = progress;
            this.SliderText = sliderText;
            this.LoadingText = loadingText;
        }
    }

    class ProgressPanel : MonoBehaviour
    {
        public const string ASSET_BUNDLE_NAME = "modtekassetbundle";

        public class ProgressBarLoadingBehavior : MonoBehaviour
        {
            public Text SliderText { get; set; }
            public Text LoadingText{ get; set; }
            public Slider Slider { get; set; }
            public Action FinishAction { get; set; }

            private LinkedList<Func<IEnumerator<ProgressReport>>> WorkList = new LinkedList<Func<IEnumerator<ProgressReport>>>();

            void Awake()
            {
                this.Slider = this.gameObject.GetComponent<Slider>();
            }

            void Start()
            {
                StartCoroutine(RunWorkList());
            }

            public void SubmitWork(Func<IEnumerator<ProgressReport>> work)
            {
                this.WorkList.AddLast(work);
            }

            IEnumerator RunWorkList()
            {
                foreach (var workFunc in WorkList) {
                    IEnumerator<ProgressReport> workEnumerator = workFunc.Invoke();
                    while (workEnumerator.MoveNext())
                    {
                        ProgressReport report = workEnumerator.Current;
                        this.Slider.value = report.Progress;
                        this.SliderText.text = report.SliderText;
                        this.LoadingText.text = report.LoadingText;
                        yield return null;
                    }
                    yield return null;
                }

                this.Slider.value = 1.0f;
                this.SliderText.text = "Done";
                this.LoadingText.text = "";
                yield return null;

                // Let Finished stay on the screen for a moment
                Thread.Sleep(1000);
                try
                {
                    FinishAction.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("Exception during ModTek: RunWorkFunc {0}", e));
                }

                yield break;
            }
        }

        
        private static AssetBundle ProgressBarAssetBundle = null;

        private static void InitializeAssets(string assetDirectory)
        {
            if (ProgressBarAssetBundle == null)
            {
                // Load the additional progress bar bundle located in the mod directory
                ProgressBarAssetBundle = LoadAssets(assetDirectory, ASSET_BUNDLE_NAME);
                if (ProgressBarAssetBundle == null)
                {
                    string message = string.Format("Error loading asset bundle {0}", ASSET_BUNDLE_NAME);
                    Logger.LogWithDate(message);
                    throw new IOException(message);
                }
            }
        }

        private static AssetBundle LoadAssets(string directory, string bundleName)
        {
            string name = Path.Combine(directory, bundleName);
            Logger.Log(string.Format("Attempting to load asset bundle: {0}", name));
            return AssetBundle.LoadFromFile(name);
        }


        public static void ShowPanel(string panelTitle)
        {
            try
            {   if (ProgressBarAssetBundle == null)
                {
                    Logger.LogWithDate("Assets not loaded. Cannot display panel!");
                    return;
                }

                var canvasPrefab = ProgressBarAssetBundle.LoadAsset<GameObject>("ProgressBar_Canvas");
                GameObject canvasGO = Instantiate(canvasPrefab);

                GameObject panelTitleGO = GameObject.Find("ProgressBar_Title");
                if (panelTitleGO != null)
                {
                    panelTitleGO.GetComponent<Text>().text = panelTitle;
                }

                GameObject progressBarSliderTextGO = GameObject.Find("ProgressBar_Slider_Text");
                Text progressBarSliderText = progressBarSliderTextGO != null ? progressBarSliderTextGO.GetComponent<Text>() : null;
                if (progressBarSliderText == null)
                {
                    Logger.LogWithDate("Error loading ProgressBar_Slider_Text");
                    return;
                }

                GameObject progressBarLoadingTextGO = GameObject.Find("ProgressBar_Loading_Text");
                Text progressBarLoadingText = progressBarLoadingTextGO != null ? progressBarLoadingTextGO.GetComponent<Text>() : null;
                if (progressBarLoadingText == null)
                {
                    Logger.LogWithDate("Error loading ProgressBar_Loading_Text");
                    return;
                }

                // Hook up the progress behavior to ProgressBar_Slider;
                GameObject progressBarSliderGO = GameObject.Find("ProgressBar_Slider");
                if (progressBarSliderGO != null)
                {
                    ProgressBarLoadingBehavior progressBarLoadingBehavior = progressBarSliderGO.AddComponent<ProgressBarLoadingBehavior>();
                    progressBarLoadingBehavior.SliderText = progressBarSliderText;
                    progressBarLoadingBehavior.LoadingText = progressBarLoadingText;
                    progressBarLoadingBehavior.FinishAction = (() =>
                    {
                        ProgressBarAssetBundle.Unload(true);
                        Destroy(canvasGO);
                        TriggerGameLoading();
                    });
                }
            }
            catch (Exception e)
            {
                Logger.LogWithDate(string.Format("Excception encountered initializing panel. Exception={0}", e));
            }
        }


        // Reactivate the main menu loading by calling the attached ActivateAndClose behavior on the UnityGameInstance (initializes a handful of different things);
        private static void TriggerGameLoading()
        {
            var activateAfterInit = GameObject.Find("Main").GetComponent<ActivateAfterInit>();
            Traverse.Create(activateAfterInit).Method("ActivateAndClose").GetValue();
        }

        public static void SubmitWork(Func<IEnumerator<ProgressReport>> workFunc)
        {
            // Hook up the progress behavior to ProgressBar_Slider;
            GameObject progressBarSliderGO = GameObject.Find("ProgressBar_Slider");
            if (progressBarSliderGO != null)
            {
                ProgressBarLoadingBehavior progressBarLoadingBehavior = progressBarSliderGO.GetComponent<ProgressBarLoadingBehavior>();
                progressBarLoadingBehavior.SubmitWork(workFunc);
            }
            else
            {
                Logger.Log("ProgressPanel not found: Performing work on current thread");
                IEnumerator<ProgressReport> workEnumerator = workFunc.Invoke();
                while (workEnumerator.MoveNext())
                {
                    // do nothing -- drain the enumeration -- complete the work on this thread immediately
                }
            }
        }

        public static bool Initialize(string directory, string panelTitle)
        {
            try
            {
                // Load the panel assets
                InitializeAssets(directory);

                // Instantiates the panel assets and displays them on screen
                ShowPanel(panelTitle);

                return true;
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("Exception caught: {0}", e));
                return false;
            }
        }
    }
}
