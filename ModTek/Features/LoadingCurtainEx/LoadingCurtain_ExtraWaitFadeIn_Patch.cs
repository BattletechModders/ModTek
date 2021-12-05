using BattleTech.UI;
using Harmony;
using ModTek.UI;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace ModTek.Features.LoadingCurtainEx
{
    /// <summary>
    /// Clear the LoadingCurtainErrorText when loading curtain hides using ExtraWaitFadeIn
    /// </summary>
    // [HarmonyPatch(typeof(LoadingCurtain), "ExtraWaitFadeIn")]
    internal static class LoadingCurtain_ExtraWaitFadeIn_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix()
        {
            LoadingCurtainErrorText.Clear();
        }
    }
}
