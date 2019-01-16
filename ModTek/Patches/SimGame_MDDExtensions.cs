using BattleTech.Data;
using Harmony;
using System.IO;

namespace ModTek
{
    /// <summary>
    /// Patch the UpdateContract MDD to fix it so that the fileID instead of the path that is passed to it
    /// If this wasn't done, all mod contracts would be incorrectly added to the DB
    /// </summary>
    [HarmonyPatch(typeof(SimGame_MDDExtensions), "UpdateContract")]
    public static class SimGame_MDDExtensions_UpdateContract_Patch
    {
        public static void Prefix(ref string fileID)
        {
            if (Path.IsPathRooted(fileID))
                fileID = Path.GetFileNameWithoutExtension(fileID);
        }
    }
}
