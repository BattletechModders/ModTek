using System;
using System.ComponentModel;
using System.IO;
using BattleTech;
using ModTek.Misc;
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

        internal DateTime FileVersion => File.GetLastAccessTimeUtc(Path);
        public string FileNameWithoutExtension => System.IO.Path.GetFileNameWithoutExtension(Path);
        public string FileName => System.IO.Path.GetFileName(Path);
        public string[] Files => Directory.GetFiles(Path);

        public bool IsFile => File.Exists(Path);
        public bool IsDirectory => Directory.Exists(Path);

        public string RelativePathToMods => FileUtils.GetRelativePath(Path, FilePaths.ModsDirectory);

        public bool IsJson => Path?.EndsWith(".json") ?? false;
        public bool IsTxt => Path?.EndsWith(".txt") ?? false;
        public bool IsCsv => Path?.EndsWith(".csv") ?? false;

        public string Type { get; set; }
        public BattleTechResourceType? ResourceType => Enum.TryParse<BattleTechResourceType>(Type, out var resType) ? resType : null;
        public bool IsResourceType => ResourceType != null;

        public string Id { get; set; }
        public bool HasId => !string.IsNullOrEmpty(Id);

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

        public VersionManifestEntry GetVersionManifestEntry()
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
