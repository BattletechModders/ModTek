using BattleTech;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch the LoadDefaultManifest to use the cached manifest that is built at ModTek load instead of rebuilding it
    /// This is primarily a performance optimization
    /// </summary>
    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static bool Prefix(ref VersionManifest __result)
        {
            if (ModTek.CachedVersionManifest == null)
                return true;

            __result = ModTek.CachedVersionManifest;
            return false;
        }
        public static void Postfix(ref VersionManifest __result)
        {
            if (ModTek.CachedVersionManifest == null)
            {
                ModTek.CachedVersionManifest = __result;
                RuntimeLog.RLog.M.TWL(0, "Updating CachedVersionManifest");
            }

        }
    }
}
