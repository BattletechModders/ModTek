using System.Linq;
using BattleTech;

namespace ModTek.Features.Manifest.MDD
{
    internal static class VersionManifestEntryExtensions
    {
        // this is to determine if something is already in the unmodified MDDB
        // a more proper way would be to query the default manifest and the temporarily loaded DLC in BetterBTRL
        internal static bool IsVanillaOrDlc(this VersionManifestEntry entry)
        {
            return entry.IsMemoryAsset || entry.IsResourcesAsset || entry.IsStreamingAssetData() || entry.IsContentPackAssetBundle();
        }

        private static bool IsStreamingAssetData(this VersionManifestEntry entry)
        {
            return entry.IsFileAsset && (entry.GetRawPath()?.StartsWith("data/") ?? false);
        }

        private static bool IsContentPackAssetBundle(this VersionManifestEntry entry)
        {
            return entry.IsAssetBundled && HBSContentNames.Contains(entry.AssetBundleName);
        }

        // possibly not complete
        private static readonly string[] HBSContentNames =
        {
            "shadowhawkdlc",
            "flashpoint",
            "urbanwarfare",
            "heavymetal"
        };

        internal static string ToShortString(this VersionManifestEntry entry)
        {
            return $"{entry.Id} ({entry.Type})";
        }
    }
}
