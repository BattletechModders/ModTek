using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS;

using ModTek.Features.Manifest.Patches;
using UnityEngine.Video;
using static ModTek.Features.Logging.MTLogger;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ModTek.Features.Manifest
{
    internal class ModsManifestPreloader
    {
        internal static int finishedChecksAndPreloadsCounter;
        private static readonly Stopwatch preloadSW = new();
        internal static bool isPreloading => preloader != null;
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
            preloader.StartWaiting();
        }

        private static void FinalizePreloadResources()
        {
            finishedChecksAndPreloadsCounter++;
            preloader = null;
            preloadSW.Stop();
            LogIfSlow(preloadSW, "Preloading");
        }

        private readonly DataManager dataManager = UnityGameInstance.BattleTechGame.DataManager;
        private readonly HashSet<BattleTechResourceType> loadingTypes = new();
        private readonly HashSet<CacheKey> loadingResources = new();

        private readonly bool rebuildMDDB;
        private readonly HashSet<CacheKey> preloadResources;

        private LoadRequest loadRequest;
        private ModsManifestPreloader(bool rebuildMDDB, HashSet<CacheKey> preloadResources)
        {
            this.rebuildMDDB = rebuildMDDB;
            this.preloadResources = preloadResources;
        }

        private void StartWaiting()
        {
            LoadingCurtain.ExecuteWhenVisible(() =>
            {
                if (dataManager.IsLoading)
                {
                    UnityGameInstance.BattleTechGame.MessageCenter.AddFiniteSubscriber(
                        MessageCenterMessageType.DataManagerLoadCompleteMessage,
                        _ =>
                        {
                            StartLoading();
                            return true;
                        }
                    );
                }
                else
                {
                    StartLoading();
                }
            });
            ShowLoadingCurtainForMainMenuPreloading();
        }

        private void StartLoading()
        {
            try
            {
                loadRequest = dataManager.CreateLoadRequest(_ => PreloadFinished());

                PreparePrewarmRequests();
                PreparePreloadRequests();

                loadRequest.ProcessRequests();
            }
            catch (Exception e)
            {
                Log("ERROR: Couldn't start loading via preload", e);
            }
        }

        private void PreparePrewarmRequests()
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
                if (!prewarm.PrewarmAllOfType)
                {
                    continue;
                }

                if (loadingTypes.Add(prewarm.ResourceType))
                {
                    Log($"\tPrewarming resources of type {prewarm.ResourceType}.");
                    AddPrewarmRequest(prewarm);
                }
            }

            foreach (var prewarm in prewarmRequests)
            {
                if (prewarm.PrewarmAllOfType)
                {
                    continue;
                }

                if (loadingTypes.Contains(prewarm.ResourceType))
                {
                    continue;
                }

                var cacheKey = new CacheKey(prewarm.ResourceType.ToString(), prewarm.ResourceID);
                if (loadingResources.Add(cacheKey))
                {
                    Log($"\tPrewarming resource {cacheKey}.");
                    AddPrewarmRequest(prewarm);
                }
            }
        }

        private void AddPrewarmRequest(PrewarmRequest prewarm)
        {
            // does double instantiation, not sure if a good idea (potential CC issues) but is vanilla behavior
            //loadRequest.AddPrewarmRequest(prewarmRequest);

            // has no double instantiation, not vanilla
            if (prewarm.PrewarmAllOfType)
            {
                loadRequest.AddAllOfTypeBlindLoadRequest(prewarm.ResourceType, true);
            }
            else
            {
                loadRequest.AddBlindLoadRequest(prewarm.ResourceType, prewarm.ResourceID, true);
            }
        }

        private void PreparePreloadRequests()
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
                foreach (var type in BTConstants.MDDBTypes.ToHashSet())
                {
                    if (!loadingTypes.Add(type))
                    {
                        continue;
                    }

                    foreach (var entry in dataManager.ResourceLocator.AllEntriesOfResource(type))
                    {
                        if (entry.IsTemplate)
                        {
                            continue;
                        }

                        if (loadingResources.Add(new CacheKey(entry)))
                        {
                            AddPreloadRequest(type, entry.Id);
                        }
                    }
                }
            }
            else
            {
                foreach (var resource in preloadResources)
                {
                    if (!BTConstants.ResourceType(resource.Type, out var resourceType))
                    {
                        continue;
                    }

                    if (loadingTypes.Contains(resourceType))
                    {
                        continue;
                    }

                    if (loadingResources.Add(resource))
                    {
                        AddPreloadRequest(resourceType, resource.Id);
                    }
                }
            }
        }

        private void AddPreloadRequest(BattleTechResourceType resourceType, string resourceId)
        {
            loadRequest.AddBlindLoadRequest(resourceType, resourceId, true);
        }

        private void PreloadFinished()
        {
            try
            {
                Log("Preloader finished");
                if (ModTek.Config.DelayPrewarmUntilPreload)
                {
                    Traverse.Create(dataManager).Method("PrewarmComplete", loadRequest).GetValue();
                }

                ModsManifest.SaveCaches();
                FinalizePreloadResources();
            }
            catch (Exception e)
            {
                Log("ERROR can't fully finish preload", e);
            }
        }

        private static void ShowLoadingCurtainForMainMenuPreloading()
        {
            Log("Showing LoadingCurtain for Preloading.");
            LoadingCurtain.ShowPopupUntil(
                PopupClosureConditionalCheck,
                "Indexing modded data, might take a while.\nGame can temporarily freeze."
            );
        }

        private static bool PopupClosureConditionalCheck()
        {
            try
            {
                var condition = !isPreloading && !UnityGameInstance.BattleTechGame.DataManager.IsLoading;

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
