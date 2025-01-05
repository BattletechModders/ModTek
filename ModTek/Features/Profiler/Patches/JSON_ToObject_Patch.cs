using fastJSON;
using ModTek.Util.Stopwatch;

namespace ModTek.Features.Profiler.Patches;

[HarmonyPatch(typeof(JSON), nameof(JSON.ToObject), typeof(string), typeof(bool))]
internal static class JSON_ToObject_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled && ModTek.Config.ProfilerEnabled;
    }

    private static readonly MTStopwatchWithCallback s_stopwatch = new(stats =>
        {
            Log.Main.Trace?.Log(
                $"JSON.ToObject called {stats.Count} times, taking a total of {stats.TotalTime} with an average of {stats.AverageNanoseconds}ns."
            );
        }
    );

    [HarmonyPriority(Priority.First)]
    public static void Prefix(ref long __state)
    {
        __state = MTStopwatch.GetTimestamp();
    }

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(ref long __state)
    {
        s_stopwatch.EndMeasurement(__state);
    }
}