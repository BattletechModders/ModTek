using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using BattleTech;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModTek
{
    public class ModDef : IModDef
    {
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
        public HashSet<string> DependsOn { get; set; } = new HashSet<string>();
        public HashSet<string> ConflictsWith { get; set; } = new HashSet<string>();

        // adding and running code
        public string DLL { get; set; }
        public string DLLEntryPoint { get; set; }

        // changing implicit loading behavior
        [DefaultValue(true)]
        public bool LoadImplicitManifest { get; set; } = true;

        // manifest, for including any kind of things to add to the game's manifest
        public List<ManifestEntry> Manifest { get; set; } = new List<ManifestEntry>();

        // a settings file to be nice to our users and have a known place for settings
        // these will be different depending on the mod obviously
        public JObject Settings { get; set; } = new JObject();

        /// <summary>
        ///     Creates a ModDef from a path to a mod.json
        /// </summary>
        /// <param name="path">Path to mod.json</param>
        /// <returns>A ModDef representing the mod.json</returns>
        public static ModDef CreateFromPath(string path)
        {
            var modDef = JsonConvert.DeserializeObject<ModDef>(File.ReadAllText(path));
            modDef.Directory = Path.GetDirectoryName(path);
            return modDef;
        }

        public class ManifestEntry : IManifestEntry
        {
            [JsonConstructor]
            public ManifestEntry(string path, bool shouldMergeJSON = false)
            {
                Path = path;
                ShouldMergeJSON = shouldMergeJSON;
            }

            public ManifestEntry(ManifestEntry parent, string path, string id)
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
                if (versionManifestEntry == null)
                    versionManifestEntry = new VersionManifestEntry(Id, Path, Type, DateTime.Now, "1", AssetBundleName, AssetBundlePersistent);

                return versionManifestEntry;
            }
        }
    }
}
