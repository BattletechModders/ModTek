using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BattleTech;
using BattleTech.Assetbundles;
using BattleTech.Data;
using Harmony;
using JetBrains.Annotations;

namespace ModTek
{
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(VersionInfo), "GetReleaseVersion")]
    public static class VersionInfo_GetReleaseVersion_Patch
    {
        [UsedImplicitly]
        public static void Postfix(ref string __result)
        {
            var old = __result;
            __result = old + $" w/ ModTek v{Assembly.GetExecutingAssembly().GetName().Version}";
        }
    }

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFilepath")]
    public static class AssetBundleManager_AssetBundleNameToFilepath_Patch
    {
        [UsedImplicitly]
        public static void Postfix(string assetBundleName, ref string __result)
        {
            if (ModTek.ModAssetBundlePaths.ContainsKey(assetBundleName))
            {
                __result = ModTek.ModAssetBundlePaths[assetBundleName];
            }
        }
    }

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFileURL")]
    public static class AssetBundleManager_AssetBundleNameToFileURL_Patch
    {
        [UsedImplicitly]
        public static void Postfix(string assetBundleName, ref string __result)
        {
            if (ModTek.ModAssetBundlePaths.ContainsKey(assetBundleName))
            {
                __result = $"file://{ModTek.ModAssetBundlePaths[assetBundleName]}";
            }
        }
    }

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        [UsedImplicitly]
        public static void Postfix(VersionManifest __result)
        {
            ModTek.TryAddToVersionManifest(__result);
        }
    }

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(DataManager), new[] { typeof(MessageCenter) })]
    public static class DataManager_CTOR_Patch
    {
        [UsedImplicitly]
        public static void Prefix()
        {
            ModTek.LoadMods();
        }
    }
}
