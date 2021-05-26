using System;
using System.ComponentModel;
using System.IO;
using BattleTech;
using ModTek.Misc;
using ModTek.Mods;
using ModTek.SoundBanks;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Manifest
{
    internal class ModEntry
    {
        [JsonConstructor]
        public ModEntry(string path, bool shouldMergeJSON = false)
        {
            Path = path;
            ShouldMergeJSON = shouldMergeJSON;
        }

        public ModEntry(ModEntry parent, string path, string id)
        {
            Path = path;
            Id = id;

            Type = parent.Type;
            AssetBundleName = parent.AssetBundleName;
            AssetBundlePersistent = parent.AssetBundlePersistent;
            ShouldMergeJSON = parent.ShouldMergeJSON;
            ShouldAppendText = parent.ShouldAppendText;
            AddToAddendum = parent.AddToAddendum;
            AddToDB = parent.AddToDB;
        }

        [JsonProperty(Required = Required.Always)]
        public string Path { get; set; }

        // directory based methods, used during normalization
        public bool IsDirectory => Directory.Exists(Path);
        public string[] Files => Directory.GetFiles(Path);

        // file based methods
        public bool IsFile => File.Exists(Path);
        internal DateTime FileVersion => File.GetLastAccessTimeUtc(Path);
        public string FileExtension => System.IO.Path.GetExtension(Path);
        public string FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);
        internal string RelativePathToMods => FileUtils.GetRelativePath(FilePaths.ModsDirectory, Path);

        internal bool IsJson => Path?.EndsWith(".json") ?? false;
        internal bool IsTxt => Path?.EndsWith(".txt") ?? false;
        internal bool IsCsv => Path?.EndsWith(".csv") ?? false;

        public string Type { get; set; }
        internal bool IsTypeStreamingAsset => Type == null;
        internal bool IsTypeCustomResource => Type != null && ModsManifest.CustomResources.ContainsKey(Type);
        internal bool IsTypeSoundBankDef => Type == nameof(SoundBankDef);
        internal bool IsTypeCustomTag => Type == ModDefExLoading.CustomType_Tag;
        internal bool IsTypeCustomTagSet => Type == ModDefExLoading.CustomType_TagSet;
        internal BattleTechResourceType? ResourceType => Enum.TryParse<BattleTechResourceType>(Type, out var resType) ? resType : null;
        internal bool IsTypeBattleTechResourceType => ResourceType != null;

        public string Id { get; set; }
        internal bool HasId => !string.IsNullOrEmpty(Id);

        public string AddToAddendum { get; set; }
        public string AssetBundleName { get; set; }
        public bool? AssetBundlePersistent { get; set; }

        [DefaultValue(false)]
        public bool ShouldMergeJSON { get; set; }

        [DefaultValue(false)]
        public bool ShouldAppendText { get; set; }

        [DefaultValue(true)]
        public bool AddToDB { get; set; } = true;

        [JsonIgnore]
        private VersionManifestEntry versionManifestEntry;

        internal VersionManifestEntry GetVersionManifestEntry()
        {
            return versionManifestEntry ??= new VersionManifestEntry(
                Id,
                Path,
                Type,
                DateTime.Now,
                "1",
                AssetBundleName,
                AssetBundlePersistent
            );
        }
    }
}
