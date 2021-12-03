using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS;
using ModTek.Features.LoadingCurtainEx;
using ModTek.Features.Manifest.MDD;
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
        internal static void PreloadResources(bool rebuildMDDB, HashSet<CacheKey> preloadResources)
        {
            preloadSW.Start();

            var dataManager = UnityGameInstance.BattleTechGame.DataManager;
            var loadRequest = dataManager.CreateLoadRequest(_ => PreloadFinished());

            var loadingTypes = new HashSet<BattleTechResourceType>();
            var loadingResources = new HashSet<CacheKey>();

            PreparePrewarmRequests(
                loadRequest,
                loadingTypes,
                loadingResources
            );

            PreparePreloadRequests(
                loadRequest,
                dataManager,
                rebuildMDDB,
                preloadResources,
                loadingTypes,
                loadingResources
            );

            if (loadRequest.GetRequestCount() == 0)
            {
                Log("Nothing to pre-warm or pre-load.");
                FinalizePreloadStats();
                return;
            }

            // GenericPopupBuilder
            //     .Create("Pre-load required", "")
            //     .AddButton(
            //         "Continue",
            //         () =>
            //         {
            Log("Pre-warming and/or pre-loading resources.");
            isPreloading = true;
            LoadingCurtain.ExecuteWhenVisible(() => loadRequest.ProcessRequests());
            ShowLoadingCurtainForMainMenuPreloading();
            //     })
            // .Render();
        }

        private static void PreparePrewarmRequests(LoadRequest loadRequest, HashSet<BattleTechResourceType> loadingTypes, HashSet<CacheKey> loadingResources)
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
                    AddPrewarmRequest(loadRequest, prewarm);
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
                    AddPrewarmRequest(loadRequest, prewarm);
                }
            }

            isPrewarmRequestedForNextPreload = false;
        }

        private static void AddPrewarmRequest(LoadRequest loadRequest, PrewarmRequest prewarm)
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

        private static void PreparePreloadRequests(LoadRequest loadRequest, DataManager dataManager, bool rebuildMDDB, HashSet<CacheKey> preloadResources, HashSet<BattleTechResourceType> loadingTypes, HashSet<CacheKey> loadingResources)
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
                            AddPreloadRequest(loadRequest, type, entry.Id);
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
                        AddPreloadRequest(loadRequest, resourceType, resource.Id);
                    }
                }
            }
        }

        private static void AddPreloadRequest(LoadRequest loadRequest, BattleTechResourceType resourceType, string resourceId)
        {
            loadRequest.AddBlindLoadRequest(resourceType, resourceId, true);
        }

        private static void PreloadFinished()
        {
            ModsManifest.SaveCaches();
            isPreloading = false;
            FinalizePreloadStats();
        }

        private static void FinalizePreloadStats()
        {
            finishedChecksAndPreloadsCounter++;
            preloadSW.Stop();
            LogIfSlow(preloadSW, "Preloading");
        }

        // TODO video pause + resume would be nice for all use cases
        private static void ShowLoadingCurtainForMainMenuPreloading()
        {
            Log("Showing LoadingCurtain for Preloading.");
            LoadingCurtain.ShowPopupUntil(
                () =>
                {
                    var videoPlayer = GetMainMenuBGVideoPlayer();
                    if (videoPlayer == null)
                    {
                        return !isPreloading;
                    }

                    if (isPreloading)
                    {
                        if (videoPlayer.isPlaying)
                        {
                            videoPlayer.Pause();
                        }
                        return false;
                    }
                    else
                    {
                        if (videoPlayer.isPaused)
                        {
                            videoPlayer.Play();
                        }
                        return true;
                    }
                },
                "Indexing modded data, might take a while."
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

        internal static void UpdateLoadingCurtainTextForProcessedEntry(VersionManifestEntry entry)
        {
            if (ModTek.Config.ShowPreloadResourcesProgress)
            {
                LoadingCurtainUtils.SetActivePopupText($"Loaded {entry.ToShortString()}");
            }
        }
    }
}
