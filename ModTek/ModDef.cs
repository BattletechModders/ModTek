using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace ModTek
{
    public class ModDef
    {
        public class ManifestEntry
        {
            [JsonProperty(Required = Required.Always)]
            public string Type { get; set; }

            [JsonProperty(Required = Required.Always)]
            public string Path { get; set; }

            public string Id { get; set; }
            public string AssetBundleName { get; set; }
            public bool? AssetBundlePersistent { get; set; }

            [DefaultValue(true)]
            public bool MergeJSON { get; set; } = true;

            public ManifestEntry(string type, string path, string id = null, string assetBundleName = null, bool? assetBundlePersistent = null)
            {
                Type = type;
                Path = path;
                Id = id;
                AssetBundleName = assetBundleName;
                AssetBundlePersistent = assetBundlePersistent;
            }
        }

        // this path will be set at runtime by ModTek
        [JsonIgnore]
        public string Directory { get; set; }

        // name will probably have to be unique
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        // versioning
        public string Version { get; set; }
        public DateTime? PackagedOn { get; set; }
        
        // this will abort loading by ModTek if set to false
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;
        
        // load order
        public List<string> DependsOn { get; set; }
        public List<string> ConflictsWith { get; set; }

        // adding and running code
        public string DLL { get; set; }
        public string DLLEntryPoint { get; set; }

        // changing implicit loading behavior
        [DefaultValue(true)]
        public bool LoadImplicitManifest { get; set; } = true;

        // manifest, for including any kind of things to add to the game's manifest
        public List<ManifestEntry> Manifest { get; set; }

        // a settings file to be nice to our users and have a known place for settings
        // these will be different depending on the mod obviously
        public JObject Settings { get; set; }
    }
}
