using System.Collections.Generic;
using System.Diagnostics;
using BattleTech;
using BattleTech.UI;
using ModTek.Features.LoadingCurtainEx;
using ModTek.Features.Manifest.MDD;
using UnityEngine.Video;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest
{
    internal static class ModsManifestPreloader
    {
        private static readonly Stopwatch preloadSW = new();
        private static bool isPreloading;
        internal static void PreloadResources(bool rebuildMDDB, HashSet<CacheKey> preloadResources)
        {
            preloadSW.Start();
            isPreloading = true;

            Log("Preloading resources.");

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
            loadRequest.ProcessRequests();
        }

        private static void PreloadFinished()
        {
            ModsManifest.SaveCaches();

            isPreloading = false;
            preloadSW.Stop();
            LogIfSlow(preloadSW, "Preloading");
        }

        internal static void ShowLoadingCurtainIfStillPreloading(VideoPlayer videoPlayer)
        {
            if (!isPreloading)
            {
                return;
            }
            Log("Showing LoadingCurtain in MainMenu since still pre-loading.");
            LoadingCurtain.ShowPopupUntil(
                () =>
                {
                    if (isPreloading)
                    {
                        // TODO also pause + resume for pre-warm and other mods still doing things: how to detect?
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
                "Pre-loading mod data, might take a while"
            );
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
