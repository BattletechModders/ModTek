using Newtonsoft.Json;
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
            public string Type;

            [JsonProperty(Required = Required.Always)]
            public string Path;

            public string ID;

            public ManifestEntry(string type, string path, string id = null)
            {
                Type = type;
                Path = path;
                ID = id;
            }
        }

        // this path will be set at runtime by ModTek
        [JsonIgnore]
        public string Directory { get; set; }

        // name will probably have to be unique
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        // this will abort loading by ModTek if set to false
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        // versioning
        public string Version { get; set; }
        public DateTime? PackagedOn { get; set; }

        // load order
        public List<string> DependsOn { get; set; }
        public List<string> LoadBefore { get; set; }
        public List<string> LoadAfter { get; set; }
        public List<string> ConflictsWith { get; set; }

        // adding and running code
        public string DLL { get; set; }
        public string DLLEntryPoint { get; set; }

        // ignoring stuff, so that it doesn't get loaded
        public List<string> IgnoreDirectories { get; set; }
        public List<string> IgnoreFiles { get; set; }

        // manifest, for including any kind of things to add to the game's manifest
        public List<ManifestEntry> Manifest { get; set; }

        // a settings file to be nice to our users and have a known place for settings
        // these will be different depending on the mod obviously
        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        public ModDef() { }
    }
}
