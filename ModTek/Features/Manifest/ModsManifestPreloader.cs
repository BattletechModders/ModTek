using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using HBS;
using ModTek.Features.LoadingCurtainEx.DataManagerStats;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;
using ModTek.Features.Manifest.Patches;
using UnityEngine.Video;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ModTek.Features.Manifest;

internal class ModsManifestPreloader
{
    internal static int finishedChecksAndPreloadsCounter;
    private static readonly Stopwatch preloadSW = new Stopwatch();
    internal static bool HasPreloader => preloader != null;
    private static ModsManifestPreloader preloader;

    internal static void PrewarmResourcesIfEnabled()
    {
        if (preloader != null)
        {
            Log.Main.Info?.Log("ERROR: Can't start prewarming, prewarm load request exists already");
            return;
        }

        if (!ModTek.Config.DelayPrewarmToMainMenu)
        {
            Log.Main.Info?.Log("Prewarm delay disabled.");
            return;
        }

        var prewarmRequests = DataManager_ProcessPrewarmRequests_Patch.GetAndClearPrewarmRequests();
        if (prewarmRequests.Count == 0)
        {
            Log.Main.Info?.Log("Nothing to prewarm.");
            return;
        }

        Log.Main.Info?.Log("Prewarming resources.");

        preloadSW.Start();
        preloader = new ModsManifestPreloader(prewarmRequests);
        preloader.ShowCurtain();
    }

    private static void FinalizePrewarm()
    {
        finishedChecksAndPreloadsCounter++;
        preloader = null;
        preloadSW.Stop();
        Log.Main.Info?.LogIfSlow(preloadSW, "Prewarming");
    }

    private readonly DataManager dataManager = UnityGameInstance.BattleTechGame.DataManager;
    private readonly List<PrewarmRequest> prewarmRequests;

    private ModsManifestPreloader(List<PrewarmRequest> prewarmRequests)
    {
        this.prewarmRequests = prewarmRequests;
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
        RunActionWhenDataManagerIsOrBecomesIdle(
            StartPrewarm,
            () =>
            {
                Log.Main.Info?.Log("Ongoing DataManager activity, waiting to finish before prewarm phase");
                DataManagerStats.GetStats(out var stats);
                stats.Dump();
            }
        );
    }

    private static void RunActionWhenDataManagerIsOrBecomesIdle(Action idleAction, Action stillLoadingAction = null)
    {
        var dataManager = UnityGameInstance.BattleTechGame.DataManager;
        if (dataManager.IsLoading)
        {
            stillLoadingAction?.Invoke();
            UnityGameInstance.BattleTechGame.MessageCenter.AddFiniteSubscriber(
                MessageCenterMessageType.DataManagerLoadCompleteMessage,
                _ =>
                {
                    if (dataManager.IsLoading)
                    {
                        stillLoadingAction?.Invoke();
                        return false;
                    }
                    idleAction();
                    return true;
                }
            );
        }
        else
        {
            idleAction();
        }
    }

    private void StartPrewarm()
    {
        try
        {
            AddPrewarmRequestsToQueue();
            {
                var loadRequest = dataManager.CreateLoadRequest(PrewarmComplete);
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
            Log.Main.Info?.Log("ERROR: Couldn't start loading via preload", e);
        }
    }

    private void AddPrewarmRequestsToQueue()
    {
        Log.Main.Info?.Log("Prewarming resources during preload.");
        foreach (var prewarm in prewarmRequests)
        {
            if (prewarm.PrewarmAllOfType)
            {
                Log.Main.Info?.Log($"\tPrewarming resources of type {prewarm.ResourceType}.");
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
                    Log.Main.Info?.Log($"\tPrewarming resource {entry.ToShortString()}.");
                    QueueLoadingResource(entry);
                }
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

    private void PrewarmComplete(LoadRequest loadRequest)
    {
        try
        {
            try
            {
                dataManager.PrewarmComplete(loadRequest);
            }
            catch (Exception e)
            {
                Log.Main.Info?.Log("ERROR execute PrewarmComplete", e);
            }

            FinalizePrewarm();
        }
        catch (Exception e)
        {
            Log.Main.Info?.Log("ERROR can't fully finish preload", e);
        }
    }

    private static void ShowLoadingCurtainOnMainMenu()
    {
        Log.Main.Info?.Log("Showing LoadingCurtain on Main Menu.");
        LoadingCurtain.ShowPopupUntil(
            PopupClosureConditionalCheck,
            "Prewarming game resources.\nGame can temporarily freeze."
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
                    Log.Main.Debug?.Log("Resuming MainMenu background video.");
                    videoPlayer.Play();
                }
                return true;
            }
            else
            {
                if (videoPlayer.isPlaying)
                {
                    Log.Main.Debug?.Log("Pausing MainMenu background video.");
                    videoPlayer.Pause();
                }
                return false;
            }
        }
        catch (Exception e)
        {
            Log.Main.Warning?.Log("Can't properly check if popup can be closed", e);
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
        return mainMenu.bgVideoPlayer;
    }
}