using HarmonyLib;
using HBS.Logging;

namespace HarmonyXModTest;

public static class Main
{
    private static readonly ILog s_log = Logger.GetLogger(nameof(HarmonyXModTest));
    public static void Start()
    {
        s_log.Log("Starting");

        Harmony.CreateAndPatchAll(typeof(Main));

        s_log.Log("Started");
    }

    [HarmonyPatch(typeof(VersionInfo), nameof(VersionInfo.GetReleaseVersion))]
    [HarmonyPostfix]
    [HarmonyAfter("io.github.mpstark.ModTek")]
    static void GetReleaseVersion(ref string __result)
    {
        var old = __result;
        __result = old + "\nHarmonyXModTest";
    }
}
