using BattleTech.UI;
using Harmony;
using ModTek.UI;

namespace ModTek.Features.LoadingCurtainEx
{
    /// <summary>
    /// Patch the LoadingCurtain to add error text.
    /// </summary>
    // [HarmonyPatch(typeof(LoadingCurtain), "ShowUntil")]
    internal static class LoadingCurtain_ShowUntil_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix()
        {
            LoadingCurtainErrorText.SetupActive();
        }
    }
}
