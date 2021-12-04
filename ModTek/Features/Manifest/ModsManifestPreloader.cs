using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS;
using ModTek.Features.LoadingCurtainEx;
using UnityEngine.Video;
using static ModTek.Features.Logging.MTLogger;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ModTek.Features.Manifest
{
    internal static class ModsManifestPreloader
    {
        private static readonly Stopwatch preloadSW = new();
        internal static bool isPreloading;
        internal static bool isPrewarmRequestedForNextPreload;
        internal static int finishedChecksAndPreloadsCounter;
        private static DataManager dataManager;
        private static LoadRequest loadRequest;
        internal static void PreloadResources(bool rebuildMDDB, HashSet<CacheKey> preloadResources)
        {
            if (loadRequest != null)
            {
                Log("ERROR: Can't start preloading, preload load request exists already");
                return;
            }
            preloadSW.Start();

            dataManager = UnityGameInstance.BattleTechGame.DataManager;
            loadRequest = dataManager.CreateLoadRequest(_ => PreloadFinished());

            var loadingTypes = new HashSet<BattleTechResourceType>();
            var loadingResources = new HashSet<CacheKey>();

            PreparePrewarmRequests(
                loadingTypes,
                loadingResources
            );

            PreparePreloadRequests(
                rebuildMDDB,
                preloadResources,
                loadingTypes,
                loadingResources
            );

            if (loadRequest.GetRequestCount() == 0)
            {
                Log("Nothing to pre-warm or pre-load.");
                FinalizePreload();
                return;
            }

            Log("Pre-warming and/or pre-loading resources.");
            isPreloading = true;
            LoadingCurtain.ExecuteWhenVisible(() => loadRequest.ProcessRequests());
            ShowLoadingCurtainForMainMenuPreloading();
        }

        private static void PreparePrewarmRequests(HashSet<BattleTechResourceType> loadingTypes, HashSet<CacheKey> loadingResources)
        {
            var prewarmRequests = UnityGameInstance.BattleTechGame.ApplicationConstants.PrewarmRequests;

            if (!isPrewarmRequestedForNextPreload || prewarmRequests == null || prewarmRequests.Length <= 0)
            {
                Log("Skipping prewarm during preload.");
                return;
            }

            Log("Pre-warming resources during preload.");
            foreach (var prewarm in prewarmRequests)
            {
                if (!prewarm.PrewarmAllOfType)
                {
                    continue;
                }

                if (loadingTypes.Add(prewarm.ResourceType))
                {
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
                    AddPrewarmRequest(prewarm);
                }
            }

            isPrewarmRequestedForNextPreload = false;
        }

        private static void AddPrewarmRequest(PrewarmRequest prewarm)
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

        private static void PreparePreloadRequests(bool rebuildMDDB, HashSet<CacheKey> preloadResources, HashSet<BattleTechResourceType> loadingTypes, HashSet<CacheKey> loadingResources)
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

        private static void AddPreloadRequest(BattleTechResourceType resourceType, string resourceId)
        {
            loadRequest.AddBlindLoadRequest(resourceType, resourceId, true);
        }

        private static void PreloadFinished()
        {
            ModsManifest.SaveCaches();
            isPreloading = false;
            FinalizePreload();
        }

        private static void FinalizePreload()
        {
            loadRequest = null;
            finishedChecksAndPreloadsCounter++;
            preloadSW.Stop();
            LogIfSlow(preloadSW, "Preloading");
        }

        private static void ShowLoadingCurtainForMainMenuPreloading()
        {
            Log("Showing LoadingCurtain for Preloading.");
            LoadingCurtain.ShowPopupUntil(
                () =>
                {
                    RefreshIndexingMessage();
                    var videoPlayer = GetMainMenuBGVideoPlayer();
                    if (videoPlayer == null)
                    {
                        return !isPreloading;
                    }

                    if (isPreloading)
                    {
                        if (videoPlayer.isPlaying)
                        {
                            Log("Pausing MainMenu background video.");
                            videoPlayer.Pause();
                        }
                        return false;
                    }
                    else
                    {
                        if (videoPlayer.isPaused)
                        {
                            Log("Resuming MainMenu background video.");
                            videoPlayer.Play();
                        }
                        return true;
                    }
                },
                GetIndexingMessage()
            );
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

        private static void RefreshIndexingMessage()
        {
            LoadingCurtainUtils.SetActivePopupText(GetIndexingMessage());
        }

        private const string IndexingMessageTitle = "Indexing modded data, might take a while.\nGame can temporarely freeze.";
        private static string GetIndexingMessage()
        {
            RefreshLoadRequestProgress();
            return $"{IndexingMessageTitle}{LoadRequestProgress}{ManifestProgress}";
        }

        private static string ManifestProgress = "";
        internal static void RefreshManifestProgress(VersionManifestEntry entry)
        {
            if (!ModTek.Config.ShowPreloadResourcesProgress)
            {
                return;
            }

            if (!isPreloading)
            {
                return;
            }

            ManifestProgress = "\n\n{entry.ToShortString()}";
            RefreshIndexingMessage();
        }

        private static string LoadRequestProgress = "\n";
        private static void RefreshLoadRequestProgress()
        {
            if (loadRequest == null)
            {
                return;
            }

            var pending = Traverse.Create(loadRequest).Method("GetPendingRequestCount").GetValue<int>();
            var failed = loadRequest.FailedRequests.Count;
            LoadRequestProgress = $"\nPending: {pending}";
            if (failed > 0)
            {
                LoadRequestProgress += $"\nFailed: {failed}";
            }
        }
    }
}
