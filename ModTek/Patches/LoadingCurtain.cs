using BattleTech.UI;
using Harmony;
using ModTek.UI;

// ReSharper disable UnusedMember.Global
// ReSharper disable InconsistentNaming

namespace ModTek.Patches
{
    /// <summary>
    /// Patch the LoadingCurtain to add error text.
    /// </summary>
    [HarmonyPatch(typeof(LoadingCurtain), "ShowUntil")]
    public static class LoadingCurtain_ShowUntil_Patch
    {
        public static void Postfix()
        {
            var activeInstance = Traverse.Create(typeof(LoadingCurtain)).Field("activeInstance").GetValue<LoadingCurtain>();
            LoadingCurtainErrorText.Setup(activeInstance.gameObject);
        }
    }

    /// <summary>
    /// Clear the LoadingCurtainErrorText when loading curtain hides
    /// </summary>
    [HarmonyPatch(typeof(LoadingCurtain), "Hide")]
    public static class LoadingCurtain_Hide_Patch
    {
        public static void Postfix()
        {
            LoadingCurtainErrorText.Clear();
        }
    }

    /// <summary>
    /// Clear the LoadingCurtainErrorText when loading curtain hides using ExtraWaitFadeIn
    /// </summary>
    [HarmonyPatch(typeof(LoadingCurtain), "ExtraWaitFadeIn")]
    public static class LoadingCurtain_ExtraWaitFadeIn_Patch
    {
        public static void Postfix()
        {
            LoadingCurtainErrorText.Clear();
        }
    }
}
