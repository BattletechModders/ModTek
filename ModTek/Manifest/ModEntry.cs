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

        public bool IsAssetBundlePath => Path.Equals(FilePaths.AssetBundleDirectoryName);

        // directory based methods, used during normalization
        public bool IsDirectory => Directory.Exists(AbsolutePath);

        // file based methods
        public bool IsFile => File.Exists(AbsolutePath);
        internal DateTime LastWriteTimeUtc => File.GetLastWriteTimeUtc(AbsolutePath);
        public string FileExtension => System.IO.Path.GetExtension(Path);
        public string FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);
        internal string RelativePathToMods => FileUtils.GetRelativePath(FilePaths.ModsDirectory, AbsolutePath);

        internal bool IsJson => FileUtils.IsJson(Path);
        internal bool IsTxt => FileUtils.IsTxt(Path);
        internal bool IsCsv => FileUtils.IsCsv(Path);

        public string Type { get; set; }
        internal bool IsTypeStreamingAsset => Type == null;
        internal bool IsTypeCustomResource => Type != null && ModsManifest.CustomResources.ContainsKey(Type);
        internal bool IsTypeSoundBankDef => Type == nameof(SoundBankDef);
        internal bool IsTypeCustomTag => Type == ModDefExLoading.CustomType_Tag;
        internal bool IsTypeCustomTagSet => Type == ModDefExLoading.CustomType_TagSet;
        internal BattleTechResourceType? ResourceType => BTResourceUtils.ResourceType(Type);
        internal bool IsTypeBattleTechResourceType => ResourceType != null;

        public string Id { get; set; }

        public string AddToAddendum { get; set; }
        public string[] RequiredAddendums { get; set; }
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

        [JsonIgnore]
        private VersionManifestEntry customResourceEntry;
        internal VersionManifestEntry CreateVersionManifestEntry()
        {
            return customResourceEntry ??= new VersionManifestEntry(
                Id,
                AbsolutePath,
                Type,
                LastWriteTimeUtc,
                "1",
                AssetBundleName,
                AssetBundlePersistent
            );
        }
    }
}
