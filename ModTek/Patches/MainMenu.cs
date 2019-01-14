using BattleTech.UI;
using Harmony;
using System.Linq;

namespace ModTek
{
    /// <summary>
    /// Adds popup message with all of the mods that failed to load if any.
    /// </summary>
    [HarmonyPatch(typeof(MainMenu), "Init")]
    public static class MainMenu_Init_Patch
    {
        public static void Postfix()
        {
            if (ModTek.FailedToLoadMods.Count > 0)
            {
                GenericPopupBuilder.Create("Some Mods Didn't Load", "These mods had something go wrong\nCheck .modtek/ModTek.log for more info\n\n" + string.Join(", ", ModTek.FailedToLoadMods.ToArray())).AddButton("Continue", null, true, null).Render();
                ModTek.FailedToLoadMods.Clear();
            }
        }
    }
}
