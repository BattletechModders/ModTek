using System.IO;
using BattleTech.Data;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Features.Manifest.Patches;

/// <summary>
/// Patch the UpdateContract MDD to fix it so that the fileID instead of the path that is passed to it
/// If this wasn't done, all mod contracts would be incorrectly added to the DB
/// </summary>
[HarmonyPatch(typeof(SimGame_MDDExtensions), nameof(SimGame_MDDExtensions.UpdateContract))]
internal static class SimGame_MDDExtensions_UpdateContract_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Prefix(ref string fileID)
    {
        if (Path.IsPathRooted(fileID))
        {
            fileID = Path.GetFileNameWithoutExtension(fileID);
        }
    }
}