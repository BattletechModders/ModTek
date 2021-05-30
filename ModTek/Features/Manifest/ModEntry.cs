using System;
using System.ComponentModel;
using System.IO;
using BattleTech;
using ModTek.Features.Manifest.Mods;
using ModTek.Features.SoundBanks;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Features.Manifest
{
    internal class ModEntry
    {
        [JsonProperty(Required = Required.Always)]
        public string Path { get; set; }

        public bool IsAssetBundleMergesBasePath => Path.Equals(FilePaths.AssetBundleMergesDirectoryName);

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
        internal bool IsTypeSoundBankDef => Type == nameof(SoundBankDef);
        internal BattleTechResourceType? ResourceType => BTConstants.ResourceType(Type, out var type) ? type : null;
        internal bool IsTypeBattleTechResourceType => ResourceType != null;

        public string Id { get; set; }

        public string AddToAddendum { get; set; }
        public string[] RequiredContentPacks { get; set; }
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
            return $"{Id} ({Type}): {RelativePathToMods}";
        }

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
