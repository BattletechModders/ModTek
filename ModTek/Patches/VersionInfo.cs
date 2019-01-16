using Harmony;
using System.Reflection;

namespace ModTek
{
    /// <summary>
    /// Patch the GetReleaseVersion method to tack on the ModTek version to the game version in the main menu
    /// </summary>
    [HarmonyPatch(typeof(VersionInfo), "GetReleaseVersion")]
    public static class VersionInfo_GetReleaseVersion_Patch
    {
        public static void Postfix(ref string __result)
        {
            var old = __result;
            __result = old + $" w/ ModTek v{Assembly.GetExecutingAssembly().GetName().Version}";
        }
    }
}
