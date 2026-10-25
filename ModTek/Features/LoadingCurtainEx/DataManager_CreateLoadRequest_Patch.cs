using BattleTech.Data;
using ModTek.Util.Stopwatch;

namespace ModTek.Features.LoadingCurtainEx;

[HarmonyPatch(typeof(DataManager), nameof(DataManager.CreateLoadRequest))]
internal static class DataManager_CreateLoadRequest_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled && ModTek.Config.ProfilerEnabled;
    }

    private static MTStopwatch _stopwatch =  new();

    public static void Prefix(out long __state)
    {
        __state = MTStopwatch.GetTimestamp();
    }

    public static void Postfix(DataManager __instance, ref LoadRequest __result, ref long __state)
    {
        var start = __state;
        var callback = __result.loadCompleteCallback;
        __result.loadCompleteCallback = request =>
        {
            _stopwatch.EndMeasurement(start);
            Log.Main.Info?.Log($"DataManager.CreateLoadRequest {_stopwatch.GetStats()}");
            callback?.Invoke(request);
        };
    }
}