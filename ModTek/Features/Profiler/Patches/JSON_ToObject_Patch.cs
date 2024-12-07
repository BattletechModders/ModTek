using fastJSON;
using ModTek.Features.Logging;

namespace ModTek.Features.Profiler.Patches;

[HarmonyPatch(typeof(JSON), nameof(JSON.ToObject), typeof(string), typeof(bool))]
internal static class JSON_ToObject_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled && ModTek.Config.ProfilerEnabled;
    }

    private static readonly MTStopwatch s_stopwatch = new()
    {
        Callback = stats =>
        {
            Log.Main.Trace?.Log(
                $"JSON.ToObject called {stats.Count} times, taking a total of {stats.TotalTime} with an average of {stats.AverageNanoseconds}ns."
            );
        },
        CallbackForEveryNumberOfMeasurements = 1000
    };

    [HarmonyPriority(Priority.First)]
    public static void Prefix(ref MTStopwatch.Tracker __state)
    {
        __state.Begin();
    }

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(ref MTStopwatch.Tracker __state)
    {
        s_stopwatch.AddMeasurement(__state.End());
    }
}