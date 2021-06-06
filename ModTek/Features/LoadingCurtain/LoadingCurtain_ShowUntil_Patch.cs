using Harmony;
using ModTek.UI;

namespace ModTek.Features.LoadingCurtain
{
    /// <summary>
    /// Patch the LoadingCurtain to add error text.
    /// </summary>
    [HarmonyPatch(typeof(BattleTech.UI.LoadingCurtain), "ShowUntil")]
    internal static class LoadingCurtain_ShowUntil_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix()
        {
            var activeInstance = Traverse.Create(typeof(BattleTech.UI.LoadingCurtain)).Field("activeInstance").GetValue<BattleTech.UI.LoadingCurtain>();
            LoadingCurtainErrorText.Setup(activeInstance.gameObject);
        }
    }
}
