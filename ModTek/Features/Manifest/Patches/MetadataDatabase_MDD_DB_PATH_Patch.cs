using BattleTech.Data;
using Harmony;
using ModTek.Misc;

namespace ModTek.Features.Manifest.Patches
{
    /// <summary>
    /// Patch the MDDB path to direct to the one in the .modtek path
    /// </summary>
    [HarmonyPatch(typeof(MetadataDatabase))]
    [HarmonyPatch("MDD_DB_PATH", MethodType.Getter)]
    internal static class MetadataDatabase_MDD_DB_PATH_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(ref string __result)
        {
            if (string.IsNullOrEmpty(FilePaths.ModMDDBPath))
            {
                return true;
            }

            __result = FilePaths.ModMDDBPath;
            return false;
        }
    }
}
