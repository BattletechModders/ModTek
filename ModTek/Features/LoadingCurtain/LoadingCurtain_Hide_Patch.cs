using Harmony;
using ModTek.UI;

namespace ModTek.Features.LoadingCurtain
{
    /// <summary>
    /// Clear the LoadingCurtainErrorText when loading curtain hides
    /// </summary>
    [HarmonyPatch(typeof(BattleTech.UI.LoadingCurtain), "Hide")]
    internal static class LoadingCurtain_Hide_Patch
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
