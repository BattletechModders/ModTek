using BattleTech.UI;
using Harmony;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.OnAddedToHierarchy))]
    internal static class MainMenu_OnAddedToHierarchy_Patch
    {
        public static void Postfix()
        {
            ModsManifest.ShowLoadingCurtainIfStillPreloading();
        }
    }
}
