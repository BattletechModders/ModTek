using System.Reflection;
using BattleTech;
using BattleTech.Assetbundles;
using BattleTech.Data;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek
{
    [HarmonyPatch(typeof(VersionInfo), "GetReleaseVersion")]
    public static class VersionInfo_GetReleaseVersion_Patch
    {
        public static void Postfix(ref string __result)
        {
            var old = __result;
            __result = old + $" w/ ModTek v{Assembly.GetExecutingAssembly().GetName().Version}";
        }
    }

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

    [HarmonyPatch(typeof(MetadataDatabase))]
    [HarmonyPatch("MDD_DB_PATH", PropertyMethod.Getter)]
    public static class MetadataDatabase_MDD_DB_PATH_Patch
    {
        public static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(ModTek.ModDBPath))
                return;

            __result = ModTek.ModDBPath;
        }
    }

    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        public static void Postfix(VersionManifest __result)
        {
            ModTek.AddModEntries(__result);
        }
    }

    [HarmonyPatch(typeof(DataManager), new[] { typeof(MessageCenter) })]
    public static class DataManager_CTOR_Patch
    {
        public static void Prefix()
        {
            ModTek.LoadMods();
        }
    }
}
