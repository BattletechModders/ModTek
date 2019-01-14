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
                GenericPopupBuilder.Create("Mods Failed To Load", string.Join("\n", ModTek.FailedToLoadMods.ToArray())).AddButton("Continue", null, true, null).Render();
                ModTek.FailedToLoadMods.Clear();
            }
        }
    }
}
