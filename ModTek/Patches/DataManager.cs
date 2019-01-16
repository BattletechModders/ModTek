using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;

namespace ModTek
{
    /// <summary>
    /// Patch RequestResource_Internal to add text to the loading screen if something is missing
    /// Infinite loads are a horrible fail-state, this informs the user that something has gone wrong
    /// </summary>
    [HarmonyPatch(typeof(DataManager), "RequestResource_Internal")]
    public static class DataManager_RequestResource_Internal_Patch
    {
        public static void Postfix(DataManager __instance, BattleTechResourceType resourceType, string identifier, bool __result)
        {
            if (LoadingCurtain.IsVisible && !__result)
            {
                var versionManifestEntry = __instance.ResourceLocator.EntryByID(identifier, resourceType, false);

                if (versionManifestEntry == null)
                    LoadingCurtainErrorText.AddMessage($"Missing: {identifier} ({resourceType})");
            }
        }
    }
}
