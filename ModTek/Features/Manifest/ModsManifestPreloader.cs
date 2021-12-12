using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS;
using ModTek.Features.LoadingCurtainEx;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;
using ModTek.Features.Manifest.Patches;
using UnityEngine.Video;
using static ModTek.Features.Logging.MTLogger;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ModTek.Features.Manifest
{
    internal class ModsManifestPreloader
    {
        internal static int finishedChecksAndPreloadsCounter;
        private static readonly Stopwatch preloadSW = new Stopwatch();
        internal static bool HasPreloader => preloader != null;
        private static ModsManifestPreloader preloader;

        internal static void PreloadResources(bool rebuildMDDB, HashSet<CacheKey> preloadResources)
        {
            if (preloader != null)
            {
                Log("ERROR: Can't start preloading, preload load request exists already");
                return;
            }

            Log("Prewarming and/or preloading resources.");

            preloadSW.Start();
            preloader = new ModsManifestPreloader(rebuildMDDB, preloadResources);
            preloader.ShowCurtain();
        }

        private static void FinalizePreloadResources()
        {
            finishedChecksAndPreloadsCounter++;
            preloader = null;
            preloadSW.Stop();
            LogIfSlow(preloadSW, "Preloading");
        }

        private readonly DataManager dataManager = UnityGameInstance.BattleTechGame.DataManager;

        private readonly bool rebuildMDDB;
        private readonly HashSet<CacheKey> preloadResources;

        private ModsManifestPreloader(bool rebuildMDDB, HashSet<CacheKey> preloadResources)
        {
            this.rebuildMDDB = rebuildMDDB;
            this.preloadResources = preloadResources;
        }

        private void ShowCurtain()
        {
            LoadingCurtain.ExecuteWhenVisible(StartWaiting);
            ShowLoadingCurtainOnMainMenu();
        }

        // make sure other loads are finished
        // dataManager has issues resolving dependencies if they are loaded across different loadRequests
        // e.g. if dependencies were already finished loading in another request, the new requests will be stuck waiting for dependencies forever
        private void StartWaiting()
        {
            if (dataManager.IsLoading)
            {
                UnityGameInstance.BattleTechGame.MessageCenter.AddFiniteSubscriber(
                    MessageCenterMessageType.DataManagerLoadCompleteMessage,
                    _ =>
                    {
                        // there might be new loads added by someone else during DataManagerLoadCompleteMessage, let's recheck
                        StartWaiting();
                        return true;
                    }
                );
            }
            else
            {
                StartLoading();
            }
        }

        private const string LoadingCurtainWaitText = "Waiting for the game to load into the main menu.\nGame can temporarily freeze.";
        private const string LoadingCurtainPrewarmAndPreloadText = "Prewarming and indexing modded resources.\nGame can temporarily freeze.";
        private const string LoadingCurtainPrewarmText = "Prewarming game resources.\nGame can temporarily freeze.";
        private const string LoadingCurtainPreloadText = "Indexing modded resources, might take a while.\nGame can temporarily freeze.";

        private void StartLoading()
        {
            try
            {
                AddPrewarmRequestsToQueue();
                var prewarmingCount = loadingResourcesIndex.Count;
                AddPreloadResourcesToQueue();
                var preloadingCount = loadingResourcesIndex.Count - prewarmingCount;

                if (prewarmingCount > 0 && preloadingCount > 0)
                {
                    LoadingCurtainUtils.SetActivePopupText(LoadingCurtainPrewarmAndPreloadText);
                }
                else if (prewarmingCount > 0)
                {
                    LoadingCurtainUtils.SetActivePopupText(LoadingCurtainPrewarmText);
                }
                else if (preloadingCount > 0)
                {
                    LoadingCurtainUtils.SetActivePopupText(LoadingCurtainPreloadText);
                }

                {
                    var customResourcesQueue = loadingResourcesQueue
                        .Where(e => BTConstants.ICResourceType(e.Type, out _))
                        .ToList();
                    ModsManifest.IndexCustomResources(customResourcesQueue);
                }

                {
                    var loadRequest = dataManager.CreateLoadRequest(PreloadFinished);
                    foreach (var entry in loadingResourcesQueue)
                    {
                        if (BTConstants.BTResourceType(entry.Type, out var resourceType))
                        {
                            loadRequest.AddBlindLoadRequest(resourceType, entry.Id, true);
                        }
                    }
                    loadRequest.ProcessRequests();
                }
            }
            catch (Exception e)
            {
                Log("ERROR: Couldn't start loading via preload", e);
            }
        }

        private void AddPrewarmRequestsToQueue()
        {
            if (!ModTek.Config.DelayPrewarmUntilPreload)
            {
                Log("Prewarming during preload disabled.");
                return;
            }

            var prewarmRequests = DataManager_ProcessPrewarmRequests_Patch.GetAndClearPrewarmRequests();
            if (prewarmRequests.Count == 0)
            {
                Log("Skipping prewarm during preload.");
                return;
            }

            Log("Prewarming resources during preload.");
            foreach (var prewarm in prewarmRequests)
            {
                if (prewarm.PrewarmAllOfType)
                {
                    Log($"\tPrewarming resources of type {prewarm.ResourceType}.");
                    foreach (var entry in BetterBTRL.Instance.AllEntriesOfResource(prewarm.ResourceType, true))
                    {
                        QueueLoadingResource(entry);
                    }
                }
                else
                {
                    var entry = BetterBTRL.Instance.EntryByID(prewarm.ResourceID, prewarm.ResourceType, true);
                    if (entry != null)
                    {
                        Log($"\tPrewarming resource {entry.ToShortString()}.");
                        QueueLoadingResource(entry);
                    }
                }
            }
        }

        private void AddPreloadResourcesToQueue()
        {
            if (!ModTek.Config.PreloadResourcesForCache)
            {
                Log("Skipping preload, disabled in config.");
                return;
            }

            if (!rebuildMDDB && preloadResources.Count == 0)
            {
                Log("Skipping preload, no changes detected.");
                return;
            }

            Log("Preloading resources.");
            if (rebuildMDDB)
            {
                foreach (var type in BTConstants.VanillaMDDBTypes)
                {
                    foreach (var entry in BetterBTRL.Instance.AllEntriesOfResource(type, true))
                    {
                        QueueLoadingResource(entry);
                    }
                }
            }
            foreach (var resource in preloadResources)
            {
                var entry = BetterBTRL.Instance.EntryByIDAndType(resource.Id, resource.Type, true);
                if (entry != null)
                {
                    QueueLoadingResource(entry);
                }
            }
        }

        private readonly HashSet<CacheKey> loadingResourcesIndex = new HashSet<CacheKey>();
        private readonly List<VersionManifestEntry> loadingResourcesQueue = new List<VersionManifestEntry>();
        private void QueueLoadingResource(VersionManifestEntry entry)
        {
            if (entry.IsTemplate)
            {
                return;
            }

            var key = new CacheKey(entry);
            if (loadingResourcesIndex.Add(key))
            {
                loadingResourcesQueue.Add(entry);
            }
        }

        private void PreloadFinished(LoadRequest loadRequest)
        {
            try
            {
                Log("Preloader finished");
                if (ModTek.Config.DelayPrewarmUntilPreload)
                {
                    try
                    {
                        Traverse.Create(dataManager).Method("PrewarmComplete", loadRequest).GetValue();
                    }
                    catch (Exception e)
                    {
                        Log("ERROR execute PrewarmComplete", e);
                    }
                }

                ModsManifest.SaveCaches();
                FinalizePreloadResources();
            }
            catch (Exception e)
            {
                Log("ERROR can't fully finish preload", e);
            }
        }

        private static void ShowLoadingCurtainOnMainMenu()
        {
            Log("Showing LoadingCurtain on Main Menu.");
            LoadingCurtain.ShowPopupUntil(
                PopupClosureConditionalCheck,
                LoadingCurtainWaitText
            );
        }

        private static bool PopupClosureConditionalCheck()
        {
            try
            {
                var condition = !HasPreloader;

                var videoPlayer = GetMainMenuBGVideoPlayer();
                if (videoPlayer == null)
                {
                    return condition;
                }

                if (condition)
                {
                    if (videoPlayer.isPaused)
                    {
                        Log("Resuming MainMenu background video.");
                        videoPlayer.Play();
                    }
                    return true;
                }
                else
                {
                    if (videoPlayer.isPlaying)
                    {
                        Log("Pausing MainMenu background video.");
                        videoPlayer.Pause();
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                Log("Can't properly check if popup can be closed", e);
            }
            return false;
        }

        private static VideoPlayer GetMainMenuBGVideoPlayer()
        {
            var mainMenu = LazySingletonBehavior<UIManager>.Instance.GetFirstModule<MainMenu>();
            if (mainMenu == null)
            {
                return null;
            }
            return Traverse.Create(mainMenu).Field("bgVideoPlayer").GetValue<VideoPlayer>();
        }
    }
}
