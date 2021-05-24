using BattleTech.Assetbundles;
using Harmony;
using ModTek.Manifest;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch AssetBundleNameToFilepath to load asset bundles from mod paths
    /// </summary>
    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFilepath")]
    internal static class AssetBundleManager_AssetBundleNameToFilepath_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(string assetBundleName, ref string __result)
        {
            if (!ModsManifest.ModAssetBundlePaths.ContainsKey(assetBundleName))
            {
                return;
            }

            __result = ModsManifest.ModAssetBundlePaths[assetBundleName];
        }
    }

    /// <summary>
    /// Patch AssetBundleNameToFileURL to load asset bundles from mod paths
    /// </summary>
    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFileURL")]
    internal static class AssetBundleManager_AssetBundleNameToFileURL_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(string assetBundleName, ref string __result)
        {
            if (!ModsManifest.ModAssetBundlePaths.ContainsKey(assetBundleName))
            {
                return;
            }

            __result = $"file://{ModsManifest.ModAssetBundlePaths[assetBundleName]}";
        }
    }
}
