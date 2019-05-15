using System.Linq;
using BattleTech.UI;
using Harmony;
using ModTek.Util;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Adds popup message with all of the mods that failed to load if any.
    /// </summary>
    [HarmonyPatch(typeof(MainMenu), "Init")]
    public static class MainMenu_Init_Patch
    {
        public static void Postfix()
        {
            if (ModTek.FailedToLoadMods.Count <= 0)
                return;

            GenericPopupBuilder.Create("Some Mods Didn't Load",
                    $"Check \"{ModTek.GetRelativePath(Logger.LogPath, ModTek.GameDirectory)}\" for more info\n\n"
                    + string.Join(", ", ModTek.FailedToLoadMods.ToArray()))
                .AddButton("Continue")
                .Render();
            ModTek.FailedToLoadMods.Clear();
        }
    }
}
