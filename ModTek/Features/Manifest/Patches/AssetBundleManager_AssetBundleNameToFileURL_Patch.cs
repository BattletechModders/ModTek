using BattleTech;
using BattleTech.Assetbundles;
using Harmony;
using ModTek.Features.Manifest.BTRL;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFileURL")]
    internal static class AssetBundleManager_AssetBundleNameToFileURL_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(string assetBundleName, ref string __result)
        {
            var entry = BetterBTRL.Instance.EntryByID(assetBundleName, BattleTechResourceType.AssetBundle);
            if (entry == null)
            {
                return;
            }

            __result = $"file://{entry.FilePath}";
        }
    }
}
