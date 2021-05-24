using System.Reflection;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch the GetReleaseVersion method to tack on the ModTek version to the game version in the main menu
    /// </summary>
    [HarmonyPatch(typeof(VersionInfo), "GetReleaseVersion")]
    public static class VersionInfo_GetReleaseVersion_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(ref string __result)
        {
            var old = __result;
            __result = old + $"\nw/ ModTek v{Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}";
        }
    }
}
