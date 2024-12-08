using System;
using System.Collections.Generic;
using HBS;
using HBS.Util;
using ModTek.Features.Logging;

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

    private static readonly MTStopwatch s_stopwatch  = new()
    {
        Callback = stats =>
        {
            var id = "JSONSerializationUtility.RehydrateObjectFromDictionary";
            Log.Main.Trace?.Log($"{id} was called {stats.Count} times, taking a total of {stats.TotalTime} with an average of {stats.AverageNanoseconds}ns.");
        },
        CallbackForEveryNumberOfMeasurements = 1000
    };

    [HarmonyPriority(Priority.Last)]
    public static void Prefix(string classStructure, ref MTStopwatch.Tracker __state)
    {
        if (string.IsNullOrEmpty(classStructure))
        {
            __state.Begin();
        }
    }

    [HarmonyPriority(Priority.First)]
    public static void Postfix(string classStructure, ref MTStopwatch.Tracker __state)
    {
        if (string.IsNullOrEmpty(classStructure))
        {
            s_stopwatch.AddMeasurement(__state.End());
        }
    }
}