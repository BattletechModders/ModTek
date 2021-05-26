using System.Linq;
using BattleTech.UI;
using Harmony;
using ModTek.Misc;
using ModTek.Mods;
using ModTek.Util;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Adds popup message with all of the mods that failed to load if any.
    /// </summary>
    [HarmonyPatch(typeof(MainMenu), "Init")]
    internal static class MainMenu_Init_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix()
        {
            if (ModDefsDatabase.FailedToLoadMods.Count <= 0)
            {
                return;
            }

            GenericPopupBuilder.Create(
                    "Some Mods Didn't Load",
                    $"Check \"{FileUtils.GetRelativePath(FilePaths.GameDirectory, FilePaths.LogPath)}\" for more info\n\n" + string.Join(", ", ModDefsDatabase.FailedToLoadMods.ToArray())
                )
                .AddButton("Continue")
                .Render();
            ModDefsDatabase.FailedToLoadMods.Clear();
        }
    }
}
