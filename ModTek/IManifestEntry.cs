namespace ModTek
{
    public interface IManifestEntry
    {
        string Type { get; set; }
        string Path { get; set; }
        string Id { get; set; }
        string AssetBundleName { get; set; }
        bool? AssetBundlePersistent { get; set; }

        // ReSharper disable once InconsistentNaming
        bool ShouldMergeJSON { get; set; }
    }
}