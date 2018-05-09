using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using JetBrains.Annotations;

namespace ModTek
{
    public class ModDef
        : IModDef
    {

        public class ManifestEntry : IManifestEntry
        {
            [JsonProperty(Required = Required.Always)]
            public string Path { get; set; }

            [DefaultValue(false)]
            public bool ShouldMergeJSON { get; set; }

            public string Type { get; set; }
            public string Id { get; set; }
            public string AssetBundleName { get; set; }
            public bool? AssetBundlePersistent { get; set; }
            
            [JsonConstructor]
            public ManifestEntry(string path, bool shouldMergeJSON = false, string type = null, string id = null, string assetBundleName = null,
                bool? assetBundlePersistent = null)
            {
                Path = path;
                ShouldMergeJSON = shouldMergeJSON;
                Type = type;
                Id = id;
                AssetBundleName = assetBundleName;
                AssetBundlePersistent = assetBundlePersistent;
            }

            public ManifestEntry(ManifestEntry parent, string path, string id)
            {
                Path = path;
                Id = id;

                Type = parent.Type;
                AssetBundleName = parent.AssetBundleName;
                AssetBundlePersistent = parent.AssetBundlePersistent;
                ShouldMergeJSON = parent.ShouldMergeJSON;
            }
        }

        // this path will be set at runtime by ModTek
        [JsonIgnore]
        public string Directory { get; set; }

        // name will probably have to be unique
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        // informational
        public string Description { get; set; }
        public string Author { get; set; }
        public string Website { get; set; }
        public string Contact { get; set; }

        // versioning
        public string Version { get; set; }
        public DateTime? PackagedOn { get; set; }

        // this will abort loading by ModTek if set to false
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        // load order
        public HashSet<string> DependsOn { get; [UsedImplicitly] set; } = new HashSet<string>();
        public HashSet<string> ConflictsWith { get; set; } = new HashSet<string>();

        // adding and running code
        public string DLL { get; [UsedImplicitly] set; }
        public string DLLEntryPoint { get; [UsedImplicitly] set; }

        // changing implicit loading behavior
        [DefaultValue(true)]
        public bool LoadImplicitManifest { get; set; } = true;

        // manifest, for including any kind of things to add to the game's manifest
        public List<ManifestEntry> Manifest { get; [UsedImplicitly] set; } = new List<ManifestEntry>();

        // a settings file to be nice to our users and have a known place for settings
        // these will be different depending on the mod obviously
        public JObject Settings { get; [UsedImplicitly] set; }
    }
}