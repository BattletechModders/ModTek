using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModTek
{
    public class ModDef
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
        public string BattleTechVersionMin { get; set; }
        public string BattleTechVersionMax { get; set; }
        public string BattleTechVersion { get; set; }

        // this will abort loading by ModTek if set to false
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        // load order
        public HashSet<string> DependsOn { get; set; } = new HashSet<string>();
        public HashSet<string> ConflictsWith { get; set; } = new HashSet<string>();
        public HashSet<string> OptionallyDependsOn { get; set; } = new HashSet<string>();

        [DefaultValue(false)]
        public bool IgnoreLoadFailure { get; set; } = false;

        // adding and running code
        public string DLL { get; set; }
        public string DLLEntryPoint { get; set; }

        // changing implicit loading behavior
        [DefaultValue(true)]
        public bool LoadImplicitManifest { get; set; } = true;

        // manifest, for including any kind of things to add to the game's manifest
        public List<ModEntry> Manifest { get; set; } = new List<ModEntry>();

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

        public bool AreDependanciesResolved(List<string> loaded)
        {
            return DependsOn.Count == 0 || DependsOn.Intersect(loaded).Count() == DependsOn.Count;
        }

        public bool HasConflicts(List<string> otherMods)
        {
            return ConflictsWith.Intersect(otherMods).Any();
        }
    }
}
