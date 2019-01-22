using System;
using System.ComponentModel;
using BattleTech;
using Newtonsoft.Json;

namespace ModTek
{
    public class ModEntry
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
            AddToAddendum = parent.AddToAddendum;
            AddToDB = parent.AddToDB;
        }

        [JsonProperty(Required = Required.Always)]
        public string Path { get; set; }

        [DefaultValue(false)]
        public bool ShouldMergeJSON { get; set; } // defaults to false

        [DefaultValue(true)]
        public bool AddToDB { get; set; } = true;

        public string AddToAddendum { get; set; }

        public string Type { get; set; }
        public string Id { get; set; }
        public string AssetBundleName { get; set; }
        public bool? AssetBundlePersistent { get; set; }

        private VersionManifestEntry versionManifestEntry;

        public VersionManifestEntry GetVersionManifestEntry()
        {
            return versionManifestEntry ?? (versionManifestEntry = new VersionManifestEntry(Id, Path, Type, DateTime.Now, "1", AssetBundleName, AssetBundlePersistent));
        }
    }
}
