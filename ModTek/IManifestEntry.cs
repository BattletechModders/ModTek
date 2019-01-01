using BattleTech;

namespace ModTek
{
    public interface IManifestEntry
    {
        string Type { get; set; }
        string Path { get; set; }
        string Id { get; set; }
        string AssetBundleName { get; set; }
        bool? AssetBundlePersistent { get; set; }

        bool AddToDB { get; set; }
        bool ShouldMergeJSON { get; set; }
        string AddToAddendum { get; set; }

        VersionManifestEntry GetVersionManifestEntry();
    }
}
