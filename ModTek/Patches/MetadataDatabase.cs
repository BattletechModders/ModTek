using BattleTech.Data;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch the MDDB path to direct to the one in the .modtek path
    /// </summary>
    [HarmonyPatch(typeof(MetadataDatabase))]
    [HarmonyPatch("MDD_DB_PATH", MethodType.Getter)]
    public static class MetadataDatabase_MDD_DB_PATH_Patch
    {
        public static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(ModTek.ModMDDBPath))
                return;

            __result = ModTek.ModMDDBPath;
        }
    }
}
