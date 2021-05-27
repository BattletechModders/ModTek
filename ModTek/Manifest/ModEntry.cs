using System;
using System.ComponentModel;
using System.IO;
using BattleTech;
using ModTek.Manifest.Mods;
using ModTek.Misc;
using ModTek.SoundBanks;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Manifest
{
    internal class ModEntry
    {
        [JsonProperty(Required = Required.Always)]
        public string Path { get; set; }

        public bool IsStreamingAssetPath => Path.Equals(FilePaths.StreamingAssetsDirectoryName);
        public bool IsAssetBundlePath => Path.Equals(FilePaths.AssetBundleDirectoryName);

        [JsonIgnore]
        public string AbsolutePath { get; set; }

        // directory based methods, used during normalization
        public bool IsDirectory => Directory.Exists(AbsolutePath);

        // file based methods
        public bool IsFile => File.Exists(AbsolutePath);
        internal DateTime FileVersion => File.GetLastAccessTimeUtc(AbsolutePath);
        public string FileExtension => System.IO.Path.GetExtension(Path);
        public string FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);
        internal string RelativePathToMods => FileUtils.GetRelativePath(FilePaths.ModsDirectory, AbsolutePath);

        internal bool IsJson => Path.EndsWith(".json");
        internal bool IsTxt => Path.EndsWith(".txt");
        internal bool IsCsv => Path.EndsWith(".csv");

        public string Type { get; set; }
        internal bool IsTypeStreamingAsset => Type == null;
        internal bool IsTypeCustomResource => Type != null && ModsManifest.CustomResources.ContainsKey(Type);
        internal bool IsTypeSoundBankDef => Type == nameof(SoundBankDef);
        internal bool IsTypeCustomTag => Type == ModDefExLoading.CustomType_Tag;
        internal bool IsTypeCustomTagSet => Type == ModDefExLoading.CustomType_TagSet;
        internal BattleTechResourceType? ResourceType => Enum.TryParse<BattleTechResourceType>(Type, out var resType) ? resType : null;
        internal bool IsTypeBattleTechResourceType => ResourceType != null;

        public string Id { get; set; }

        public string AddToAddendum { get; set; }
        public string AssetBundleName { get; set; }
        public bool? AssetBundlePersistent { get; set; }

        [JsonIgnore]
        public bool ShouldMergeJSON { get; set; }

        [JsonIgnore]
        public bool ShouldAppendText { get; set; }

        [DefaultValue(true)]
        public bool AddToDB { get; set; } = true;

        public ModEntry copy()
        {
            return (ModEntry) MemberwiseClone();
        }

        [JsonIgnore]
        private VersionManifestEntry versionManifestEntry;
        internal VersionManifestEntry GetVersionManifestEntry()
        {
            return versionManifestEntry ??= new VersionManifestEntry(
                Id,
                AbsolutePath,
                Type,
                DateTime.Now,
                "1",
                AssetBundleName,
                AssetBundlePersistent
            );
        }
    }
}
