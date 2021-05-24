using BattleTech.Data;
using Harmony;
using ModTek.Util;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
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

        public static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(FileUtils.ModMDDBPath))
            {
                return;
            }

            __result = FileUtils.ModMDDBPath;
        }
    }

    /// <summary>
    /// Patch the MDDB tmp path to direct to the one in the .modtek path
    /// </summary>
    [HarmonyPatch(typeof(MetadataDatabase))]
    [HarmonyPatch("MDD_TMP_PATH", MethodType.Getter)]
    internal static class MetadataDatabase_MDD_TMP_PATH_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(FileUtils.ModMDDBPath))
            {
                return;
            }

            __result = FileUtils.ModMDDBPath + ".tmp";
        }
    }
}
