using System.Collections.Generic;
using BattleTech;
using BattleTech.Data;
using Harmony;
using ModTek.Features.Logging;
using UnityEngine;

namespace ModTek.Features.LoadingCurtainEx.DataManagerStats
{
    internal class DataManagerStats
    {
        private static DataManagerStats LastStats = new DataManagerStats();
        internal static bool GetStats(out DataManagerStats stats)
        {
            var dataManager = UnityGameInstance.BattleTechGame.DataManager;
            var activeLoadBatches = Traverse
                .Create(dataManager)
                .Field("activeLoadBatches")
                .GetValue<List<LoadRequest>>();

            if (activeLoadBatches == null)
            {
                stats = null;
                return false;
            }

            stats = new DataManagerStats(activeLoadBatches);

            if (!stats.HasStats())
            {
                LastStats = null;
                return false;
            }

            if (stats.IsEqualTo(LastStats))
            {
                stats = LastStats;
                if (!stats.dumped && Time.realtimeSinceStartup - stats.time > ModTek.Config.DataManagerEverSpinnyDetectionTimespan)
                {
                    MTLogger.Info.Log("Detected stuck DataManager.");
                    stats.dumped = true;
                    stats.Dump();
                }
                return false;
            }

            LastStats = stats;
            return true;
        }

        internal readonly int batches;
        internal readonly int active;
        internal readonly int pending;
        internal readonly int completed;
        internal readonly int failed;
        internal readonly float time = Time.realtimeSinceStartup;
        internal readonly List<LoadRequest> ActiveLoadRequests;
        internal bool dumped;

        internal DataManagerStats()
        {
        }
        internal DataManagerStats(List<LoadRequest> loadRequests)
        {
            ActiveLoadRequests = loadRequests;
            foreach (var load in loadRequests)
            {
                var lrt = new LoadRequestTraverse(load);
                batches++;
                active += lrt.GetActiveRequestCount();
                pending += lrt.GetPendingRequestCount();
                completed += lrt.GetCompletedRequestCount();
                failed += lrt.instance.FailedRequests.Count;
            }
        }
        internal void Dump()
        {
            DumpLoadRequests.DumpProcessing(this);
        }

        internal bool HasStats()
        {
            return batches > 0 || active > 0 || pending > 0 || completed > 0 || failed > 0;
        }

        public override string ToString()
        {
            return $"batches={batches} active={active} pending={pending} completed={completed} failed={failed}";
        }

        internal string GetStatsTextForCurtain()
        {
            var text = $"Batches: {batches}";
            text += $"\nPending: {pending}";
            text += $"\nProcessing: {active}";
            text += $"\nCompleted: {completed}";
            if (failed > 0)
            {
                text += $"\nFailed: {failed}";
            }
            if (dumped)
            {
                text += $"\nEverspinny detected, dumped processing to log.";
            }
            return text;
        }

        public bool IsEqualTo(DataManagerStats other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (
                ActiveLoadRequests != null
                && ActiveLoadRequests.Count > 0
                && ActiveLoadRequests[0] != null
                && other.ActiveLoadRequests != null
                && other.ActiveLoadRequests.Count > 0
                && other.ActiveLoadRequests[0] != null
                && ActiveLoadRequests[0].GetHashCode() != other.ActiveLoadRequests[0].GetHashCode()
                )
            {
                return false;
            }

            return active == other.active
                && pending == other.pending
                && completed == other.completed
                && failed == other.failed
                ;
        }
    }
}
