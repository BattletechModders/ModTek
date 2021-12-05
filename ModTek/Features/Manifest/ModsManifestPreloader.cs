using System;
using System.Collections;
using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS;
using ModTek.Features.LoadingCurtainEx;
using ModTek.Features.Manifest.MDD;
using ModTek.Features.Manifest.Patches;
using UnityEngine;
using UnityEngine.Video;
using static ModTek.Features.Logging.MTLogger;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ModTek.Features.Manifest
{
    internal static class ModsManifestPreloader
    {
        private static readonly Stopwatch preloadSW = new();
        internal static bool isPreloading;
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
            var prewarmRequests = DataManager_ProcessPrewarmRequests_Patch.PrewarmRequests;
            if (prewarmRequests.Count == 0)
            {
                Log("Skipping prewarm during preload.");
                return;
            }
            DataManager_ProcessPrewarmRequests_Patch.PrewarmRequests.Clear();

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
            LoadingCurtain.ShowPopupUntil(PopupClosureConditionalCheck, GetIndexingMessage());
        }

        private static bool PopupClosureConditionalCheck()
        {
            try
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
        private static LoadStats LastStats = new();
        private static bool LastChangeDumped;
        private static void RefreshLoadRequestProgress()
        {
            if (loadRequest == null)
            {
                return;
            }

            var traverse = new LoadRequestTraverse(loadRequest);
            var stats = new LoadStats(traverse);

            LoadRequestProgress = "";
            LoadRequestProgress += $"\nPending: {stats.pending}";
            LoadRequestProgress += $"\nProcessing: {stats.active}";
            LoadRequestProgress += $"\nCompleted: {stats.completed}";
            if (stats.failed > 0)
            {
                LoadRequestProgress += $"\nFailed: {stats.failed}";
            }

            if (!stats.Equals(LastStats))
            {
                LastStats = stats;
                LastChangeDumped = false;
            }
            else if (stats.HasStats())
            {
                if (LastChangeDumped)
                {
                    LoadRequestProgress += $"\nEverspinny detected, dumped processing to log.";
                }
                else
                {
                    if (Time.realtimeSinceStartup - LastStats.time > ModTek.Config.DataManagerEverSpinnyDetectionTimespan)
                    {
                        DumpProcessing(stats);
                        //UnityGameInstance.Instance.ShutdownGame();
                        LastChangeDumped = true;
                    }
                }
            }
        }

        private static void DumpProcessing(LoadStats stats)
        {
            Log($"Detected stuck DataManager, dumping stats: {stats}");
            DumpTrackers(loadRequest, "\t");
        }

        private static void DumpTrackers(LoadRequest loadRequest, string level)
        {
            DumpTrackers(loadRequest, level, "Pending", "pendingRequests");
            DumpTrackers(loadRequest, level, "LinkedPending", "linkedPendingRequests");
            DumpTrackers(loadRequest, level, "Processing", "processingRequests");
        }
        private static void DumpTrackers(LoadRequest loadRequest, string level, string prefix, string field)
        {
            var trackers = Traverse.Create(loadRequest).Field(field).GetValue<ICollection>();
            if (trackers != null)
            {
                DumpTrackers(trackers, level, prefix);
            }
        }
        private static void DumpTrackers(ICollection trackers, string level, string prefix)
        {
            foreach (var tracker in trackers)
            {
                var message = level + prefix;

                var resource = Traverse.Create(tracker).Field<VersionManifestEntry>("resourceManifestEntry").Value;
                if (resource != null)
                {
                    message += " entry=" + resource.ToShortString();
                }

                var backing = Traverse.Create(tracker).Field<DataManager.FileLoadRequest>("backingRequest").Value;
                if (backing != null)
                {
                    message += " state=" + backing.State;
                }
                Log(message);

                var newLevel = level + "\t";
                var dependency = Traverse.Create(tracker).Field<DataManager.DependencyLoadRequest>("dependencyLoader").Value;
                if (dependency != null)
                {
                    var dependencyLoads = Traverse.Create(dependency).Field("loadRequests").GetValue<List<LoadRequest>>();
                    if (dependencyLoads != null)
                    {
                        foreach (var dependencyLoad in dependencyLoads)
                        {
                            DumpTrackers(dependencyLoad, newLevel);
                        }
                    }
                }
            }
        }

        private class LoadRequestTraverse
        {
            internal readonly LoadRequest instance;
            private readonly Traverse traverse;

            internal LoadRequestTraverse(LoadRequest instance)
            {
                this.instance = instance;
                traverse = Traverse.Create(instance);
            }

            internal int GetActiveRequestCount() => traverse.Method("GetActiveRequestCount").GetValue<int>();
            internal int GetPendingRequestCount() => traverse.Method("GetPendingRequestCount").GetValue<int>();
            internal int GetCompletedRequestCount() => traverse.Method("GetCompletedRequestCount").GetValue<int>();
        }

        private class LoadStats: IEquatable<LoadStats>
        {
            internal readonly int active;
            internal readonly int pending;
            internal readonly int completed;
            internal readonly int failed;
            internal readonly float time = Time.realtimeSinceStartup;

            internal LoadStats()
            {
            }

            internal LoadStats(LoadRequestTraverse lrt)
            {
                active = lrt.GetActiveRequestCount();
                pending = lrt.GetPendingRequestCount();
                completed = lrt.GetCompletedRequestCount();
                failed = lrt.instance.FailedRequests.Count;
            }

            internal bool HasStats()
            {
                return active > 0 || pending > 0 || completed > 0 || failed > 0;
            }

            public override string ToString()
            {
                return $"active={active} pending={pending} completed={completed} failed={failed}";
            }

            // below methods generated by Rider
            public bool Equals(LoadStats other)
            {
                if (ReferenceEquals(null, other))
                {
                    return false;
                }

                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                return active == other.active && pending == other.pending && completed == other.completed && failed == other.failed;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (obj.GetType() != this.GetType())
                {
                    return false;
                }

                return Equals((LoadStats)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = active;
                    hashCode = (hashCode * 397) ^ pending;
                    hashCode = (hashCode * 397) ^ completed;
                    hashCode = (hashCode * 397) ^ failed;
                    return hashCode;
                }
            }
        }
    }
}
