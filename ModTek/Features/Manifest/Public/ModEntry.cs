using System;
using System.ComponentModel;
using System.IO;
using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.Manifest.MDD;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace ModTek
{
    // TODO probably exposing too much as public, go through usages in RT
    public class ModEntry
    {
        [JsonProperty(Required = Required.Always)]
        public string Path { get; set; }

        // directory based methods, used during normalization
        public bool IsDirectory => Directory.Exists(AbsolutePath);

        // file based methods
        public bool IsFile => File.Exists(AbsolutePath);
        private DateTime UpdatedOn = VersionManifestEntryExtensions.UpdatedOnLazyTracking;
        internal DateTime GetUpdatedOnForTracking()
        {
            if (UpdatedOn == VersionManifestEntryExtensions.UpdatedOnLazyTracking)
            {
                UpdatedOn = File.GetLastWriteTimeUtc(AbsolutePath);
            }
            return UpdatedOn;
        }
        public string FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);
        internal string RelativePathToMods => FileUtils.GetRelativePath(FilePaths.ModsDirectory, AbsolutePath);

        internal bool IsJson => FileUtils.IsJson(Path);
        internal bool IsTxt => FileUtils.IsTxt(Path);
        internal bool IsCsv => FileUtils.IsCsv(Path);

        public string Type { get; set; }
        internal bool IsTypeCustomResource => CustomResourcesFeature.IsCustomResourceType(Type);

        public string Id { get; set; }

        public string AddToAddendum { get; set; }
        public string RequiredContentPack { get; set; }
        public string AssetBundleName { get; set; }
        public bool? AssetBundlePersistent { get; set; }

        [DefaultValue(false)]
        public bool ShouldMergeJSON { get; set; }

        [DefaultValue(false)]
        public bool ShouldAppendText { get; set; }

        [DefaultValue(true)]
        public bool AddToDB { get; set; } = true;

        public ModEntry copy()
        {
            return (ModEntry) MemberwiseClone();
        }

        [JsonIgnore]
        public ModDefEx ModDef { get; set; }

        [JsonIgnore]
        public string AbsolutePath => ModDef.GetFullPath(Path);

        public override string ToString()
        {
            var extra = Type;

            if (AddToAddendum != null)
            {
                extra += " " + nameof(AddToAddendum) + "=" + AddToAddendum;
            }
            if (AssetBundleName != null)
            {
                extra += " " + nameof(AssetBundleName) + "=" + AssetBundleName;
            }
            if (RequiredContentPack != null)
            {
                extra += " " + nameof(RequiredContentPack) + "=" + RequiredContentPack;
            }

            return $"{Id} ({extra}): {RelativePathToMods}";
        }

        public string ToShortString()
        {
            return $"{Id} ({Type})";
        }

        [JsonIgnore]
        private VersionManifestEntry customResourceEntry;
        internal VersionManifestEntry CreateVersionManifestEntry()
        {
            return customResourceEntry = customResourceEntry ?? new VersionManifestEntry(
                Id,
                AbsolutePath,
                Type,
                VersionManifestEntryExtensions.UpdatedOnLazyTracking,
                "1",
                AssetBundleName,
                AssetBundlePersistent
            );
        }
    }
}
