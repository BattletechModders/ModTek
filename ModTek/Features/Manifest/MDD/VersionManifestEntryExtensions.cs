using System.Linq;
using BattleTech;

namespace ModTek.Features.Manifest.MDD
{
    internal static class VersionManifestEntryExtensions
    {
        internal static bool IsInDefaultMDDB(this VersionManifestEntry entry)
        {
            return entry.IsResourcesAsset || entry.IsStreamingAssetData() || entry.IsContentPackAssetBundle();
        }

        internal static bool IsStreamingAssetData(this VersionManifestEntry entry)
        {
            return entry.IsFileAsset && (entry.GetRawPath()?.StartsWith("data/") ?? false);
        }

        internal static bool IsContentPackAssetBundle(this VersionManifestEntry entry)
        {
            return entry.IsAssetBundled && BTConstants.HBSContentNames.Contains(entry.AssetBundleName);
        }
    }
}
