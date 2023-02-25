using System;
using System.IO;
using BattleTech;
using BattleTech.Assetbundles;
using ModTek.Features.Manifest.BTRL;
using ModTek.Misc;

namespace ModTek.Features.Manifest.Patches;

[HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFilepath")]
internal static class AssetBundleManager_AssetBundleNameToFilepath_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static bool Prefix(string assetBundleName, ref string __result)
    {
        try
        {
            var filePath = AssetBundleNameToFilepath(assetBundleName);
            __result = filePath;
            return false;
        }
        catch (Exception e)
        {
            Log.Main.Info?.Log("Error running prefix", e);
        }
        return true;
    }

    internal static string AssetBundleNameToFilepath(string assetBundleName)
    {
        var entry = BetterBTRL.Instance.EntryByID(assetBundleName, BattleTechResourceType.AssetBundle);
        if (entry == null)
        {
            return Path.Combine(FilePaths.AssetBundlesDirectory, assetBundleName);
        }
        return Path.Combine(FilePaths.StreamingAssetsDirectory, entry.FilePath);
    }
}