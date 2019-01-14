using BattleTech;
using Harmony;

namespace ModTek
{
    /// <summary>
    /// Patch the LoadDefaultManifest to use the cached manifest that is built at ModTek load instead of rebuilding it
    /// This is primarily a performance optization
    /// </summary>
    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        public static bool Prefix(ref VersionManifest __result)
        {
            if (ModTek.CachedVersionManifest != null)
            {
                __result = ModTek.CachedVersionManifest;
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
