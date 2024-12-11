using HarmonyLib;
using HBS.Logging;

namespace ModTemplate;

public static class Main
{
    private static readonly ILog s_log = Logger.GetLogger(nameof(ModTemplate));
    public static void Start()
    {
        s_log.Log("Starting");

        // apply all patches that are in classes annotated with [HarmonyPatch]
        Harmony.CreateAndPatchAll(typeof(Main).Assembly);

        // run a specific patch found in a class which wasn't annotated with HarmonyPatch and therefore wasn't applied earlier
        Harmony.CreateAndPatchAll(typeof(Main));

        s_log.Log("Started");
    }

    [HarmonyPatch(typeof(VersionInfo), nameof(VersionInfo.GetReleaseVersion))]
    [HarmonyPostfix]
    [HarmonyAfter("io.github.mpstark.ModTek")]
    static void GetReleaseVersion(ref string __result)
    {
        var old = __result;
        __result = old + "\nMy Mod Template shows up!";
    }
}
