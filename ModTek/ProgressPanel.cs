using BattleTech;
using Harmony;
using HBS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace ModTek
{
    class ProgressReport
    {
        public float Progress { get; set; }
        public string LoadingText { get; set; }

        public ProgressReport(float progress, string loadingText)
        {
            this.Progress = progress;
            this.LoadingText = loadingText;
        }
    }

    class ProgressPanel : MonoBehaviour
    {
        private static AssetBundle LoadAssets(string directory, string bundleName)
        {
            string name = Path.Combine(directory, bundleName);
            Logger.LogWithDate(string.Format("Attempting to load asset bundle: {0}", name));
            return AssetBundle.LoadFromFile(name);
        }

        public class ProgressBarLoadingBehavior : MonoBehaviour
        {
            public Text LoadingText{ get; set; }
            public Slider Slider { get; set; }
            public Func<IEnumerator<ProgressReport>> WorkFunc { get; set; }
            public Action FinishAction { get; set; }

            void Awake()
            {
                Logger.LogWithDate("ProgressBarLoadingBehavior: Awake called");
                this.Slider = this.gameObject.GetComponent<Slider>();
            }

            void Start()
            {
                Logger.LogWithDate("ProgressBarLoadingBehavior: Start called");
                StartCoroutine(RunWorkFunc());
            }

            IEnumerator RunWorkFunc()
            {
                IEnumerator<ProgressReport> reports = WorkFunc.Invoke();
                while(reports.MoveNext())
                {
                    ProgressReport report = reports.Current;
                    this.Slider.value = report.Progress;
                    this.LoadingText.text = report.LoadingText;
                    yield return null;
                   
                }

                this.Slider.value = 1.0f;
                this.LoadingText.text = "Finished";
                yield return null;

                // Let Finished stay on the screen for a moment
                Thread.Sleep(2000);
                try
                {
                    Logger.LogWithDate("ProgressBarLoadingBehavior: Finish Action called");
                    FinishAction.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("Exception during RunWorkFunc {0}", e));
                }

                yield break;
            }
        }


        public static void InitializeProgressPanel(string assetDirectory, string panelTitle, Func<IEnumerator<ProgressReport>> workFunc)
        {
            try
            {
                // Load the additional progress bar bundle located in the mod directory
                AssetBundle assetBundle = LoadAssets(assetDirectory, "progressbarbundle");
                if (assetBundle == null)
                {
                    Logger.LogWithDate("Error loading assets");
                    return;
                }

                var canvasPrefab = assetBundle.LoadAsset<GameObject>("ProgressBar_Canvas");
                GameObject canvasGO = Instantiate(canvasPrefab);

                GameObject panelTitleGO = GameObject.Find("ProgressBar_Title");
                if (panelTitleGO != null)
                {
                    panelTitleGO.GetComponent<Text>().text = panelTitle;
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
                    Logger.LogWithDate("Setting up the progressBarBehavior");
                    ProgressBarLoadingBehavior progressBarLoadingBehavior = progressBarSliderGO.AddComponent<ProgressBarLoadingBehavior>();
                    progressBarLoadingBehavior.LoadingText = progressBarLoadingText;
                    progressBarLoadingBehavior.WorkFunc = workFunc;
                    progressBarLoadingBehavior.FinishAction = (() =>
                    {
                        assetBundle.Unload(true);
                        Destroy(canvasGO);
                        EnableMainManuLoading();
                    });
                }
            }
            catch (Exception e)
            {
                Logger.LogWithDate(string.Format("Excception encountered initializing panel. Exception={0}", e));
            }
        }

        private static void DisableMainMenuLoading()
        {
            var activateAfterInit = LazySingletonBehavior<UnityGameInstance>.Instance.gameObject.GetComponent<ActivateAfterInit>();
            activateAfterInit.enabled = false;
        }

        // Reactivate the main menu loading by calling the attached ActivateAndClose behavior on the UnityGameInstance (initializes a handful of different things);
        private static void EnableMainManuLoading()
        {
            var activateAfterInit = LazySingletonBehavior<UnityGameInstance>.Instance.gameObject.GetComponent<ActivateAfterInit>();
            Traverse.Create(activateAfterInit).Method("ActivateAndClose").GetValue();                
        }

        public static void Init(string directory)
        {
            try
            {
                // Stop BTech from going to the main menu right away
                DisableMainMenuLoading();
            }
            catch (Exception e)
            {
                Logger.Log(string.Format("Exception caught: {0}", e));
            }
        }
    }
}
