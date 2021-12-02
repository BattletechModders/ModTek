using BattleTech.UI;
using Harmony;
using UnityEngine.Video;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(MainMenu), nameof(MainMenu.OnAddedToHierarchy))]
    internal static class MainMenu_OnAddedToHierarchy_Patch
    {
        public static void Postfix(VideoPlayer ___bgVideoPlayer)
        {
            ModsManifestPreloader.ShowLoadingCurtainIfStillPreloading(___bgVideoPlayer);
        }
    }
}
