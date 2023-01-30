using Harmony;

namespace ModTemplate.Patches;

[HarmonyPatch(typeof(VersionInfo), nameof(VersionInfo.GetReleaseVersion))]
static class VersionInfo_GetReleaseVersion_Patch
{
    [HarmonyPostfix]
    [HarmonyAfter("io.github.mpstark.ModTek")]
    static void Postfix(ref string __result)
    {
        var old = __result;
        __result = old + "\nModTemplate";
    }
}
