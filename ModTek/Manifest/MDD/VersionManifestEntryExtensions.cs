using BattleTech;
using System.Linq;

namespace ModTek.Manifest.MDD
{
    internal static class VersionManifestEntryExtensions
    {
        private static readonly string[] HBSContentNames =
        {
            ShadowHawkDlcContentName,
            FlashPointContentName,
            UrbanWarfareContentName,
            HeavyMetalContentName
        };

        // probably not complete
        internal const string ShadowHawkDlcContentName = "shadowhawkdlc";
        internal const string FlashPointContentName = "flashpoint";
        internal const string UrbanWarfareContentName = "urbanwarfare";
        internal const string HeavyMetalContentName = "heavymetal";


        internal static bool IsInDefaultMDDB(this VersionManifestEntry entry)
        {
            return entry.IsFileAsset || entry.IsStreamingAssetData() || entry.IsContentPackAssetBundle();
        }

        internal static bool IsStreamingAssetData(this VersionManifestEntry entry)
        {
            return entry.GetRawPath()?.StartsWith("data/") ?? false;
        }

        internal static bool IsContentPackAssetBundle(this VersionManifestEntry entry)
        {
            return entry.IsAssetBundled && HBSContentNames.Contains(entry.AssetBundleName);
        }
    }
}
