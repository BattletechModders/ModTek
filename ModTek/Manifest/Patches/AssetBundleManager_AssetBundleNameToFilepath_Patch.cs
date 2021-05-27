using BattleTech;
using BattleTech.Assetbundles;
using Harmony;
using ModTek.Manifest.BTRL;

namespace ModTek.Manifest.Patches
{
    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFilepath")]
    internal static class AssetBundleManager_AssetBundleNameToFilepath_Patch
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

            __result = entry.FilePath;
        }
    }
}
