using BattleTech.Assetbundles;
using Harmony;

namespace ModTek
{
    /// <summary>
    /// Patch AssetBundleNameToFilepath to load asset bundles from mod paths
    /// </summary>
    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFilepath")]
    public static class AssetBundleManager_AssetBundleNameToFilepath_Patch
    {
        public static void Postfix(string assetBundleName, ref string __result)
        {
            if (!ModTek.ModAssetBundlePaths.ContainsKey(assetBundleName))
                return;

            __result = ModTek.ModAssetBundlePaths[assetBundleName];
        }
    }

    /// <summary>
    /// Patch AssetBundleNameToFileURL to load asset bundles from mod paths
    /// </summary>
    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFileURL")]
    public static class AssetBundleManager_AssetBundleNameToFileURL_Patch
    {
        public static void Postfix(string assetBundleName, ref string __result)
        {
            if (!ModTek.ModAssetBundlePaths.ContainsKey(assetBundleName))
                return;

            __result = $"file://{ModTek.ModAssetBundlePaths[assetBundleName]}";
        }
    }
}
