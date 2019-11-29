using BattleTech.Assetbundles;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch AssetBundleNameToFilepath to load asset bundles from mod paths
    /// </summary>
    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFilepath")]
    public static class AssetBundleManager_AssetBundleNameToFilepath_Patch
    {
        public static bool Prepare() { return ModTek.Enabled; }
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
        public static bool Prepare() { return ModTek.Enabled; }
        public static void Postfix(string assetBundleName, ref string __result)
        {
            if (!ModTek.ModAssetBundlePaths.ContainsKey(assetBundleName))
                return;

            __result = $"file://{ModTek.ModAssetBundlePaths[assetBundleName]}";
        }
    }
}
