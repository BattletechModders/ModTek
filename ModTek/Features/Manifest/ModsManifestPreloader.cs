using System.Collections.Generic;
using BattleTech;
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
        internal static void PreloadResources(bool rebuildMDDB, HashSet<CacheKey> preloadResources)
        {
            preloadSW.Start();

            var loadRequest = UnityGameInstance.BattleTechGame.DataManager.CreateLoadRequest(_ => PreloadFinished());
            if (rebuildMDDB)
            {
                foreach (var type in BTConstants.MDDBTypes)
                {
                    loadRequest.AddAllOfTypeBlindLoadRequest(type);
                }
            }
            else
            {
                foreach (var resource in preloadResources)
                {
                    if (BTConstants.ResourceType(resource.Type, out var resourceType))
                    {
                        loadRequest.AddBlindLoadRequest(resourceType, resource.Id);
                    }
                }
            }

            Log("Preloading resources.");
            isPreloading = true;
            ShowLoadingCurtainForMainMenuPreloading();
            loadRequest.ProcessRequests();
        }

        private static void PreloadFinished()
        {
            ModsManifest.SaveCaches();

            isPreloading = false;
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
                "Initial indexing of modded data, might take a while."
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
