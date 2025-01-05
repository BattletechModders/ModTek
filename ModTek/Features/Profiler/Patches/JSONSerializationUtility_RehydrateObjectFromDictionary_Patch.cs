using System;
using System.Collections.Generic;
using HBS;
using HBS.Util;
using ModTek.Util.Stopwatch;

namespace ModTek.Features.Profiler.Patches;

[HarmonyPatch(
    typeof(JSONSerializationUtility),
    nameof(JSONSerializationUtility.RehydrateObjectFromDictionary),
    typeof(object),
    typeof(Dictionary<string, object>),
    typeof(string),
    typeof(Stopwatch),
    typeof(Stopwatch),
    typeof(JSONSerializationUtility.RehydrationFilteringMode),
    typeof(Func<string, bool>[])
)]
internal static class JSONSerializationUtility_RehydrateObjectFromDictionary_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled && ModTek.Config.ProfilerEnabled;
    }

    private static readonly MTStopwatchWithCallback s_stopwatch  = new(stats =>
        {
            var id = "JSONSerializationUtility.RehydrateObjectFromDictionary";
            Log.Main.Trace?.Log($"{id} was called {stats.Count} times, taking a total of {stats.TotalTime} with an average of {stats.AverageNanoseconds}ns.");
        }
    );

    [HarmonyPriority(Priority.First)]
    public static void Prefix(string classStructure, ref long __state)
    {
        if (string.IsNullOrEmpty(classStructure))
        {
            __state = MTStopwatch.GetTimestamp();
        }
    }

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(string classStructure, ref long __state)
    {
        if (__state > 0)
        {
            s_stopwatch.EndMeasurement(__state);
        }
    }
}