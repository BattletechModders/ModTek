using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTek.Logging;
using ModTek.Manifest;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace ModTek.Mods
{
    internal class ModDefEx
    {
        // this path will be set at runtime by ModTek
        [JsonIgnore]
        public string Directory { get; set; }

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

        [DefaultValue(false)]
        public bool Hidden { get; set; } = false;

        [DefaultValue(false)]
        public bool Locked { get; set; } = false;

        // load order and requirements
        public HashSet<string> DependsOn { get; set; } = new();
        public HashSet<string> ConflictsWith { get; set; } = new();
        public HashSet<string> OptionallyDependsOn { get; set; } = new();

        [JsonIgnore]
        public Dictionary<ModDefEx, bool> AffectingOnline { get; set; } = new();

        [JsonIgnore]
        public Dictionary<ModDefEx, bool> AffectingOffline { get; set; } = new();

        [JsonIgnore]
        public HashSet<ModDefEx> DependsOnMe { get; set; } = new();

        [DefaultValue(false)]
        public bool IgnoreLoadFailure { get; set; }

        // adding and running code
        [JsonIgnore]
        public Assembly Assembly { get; set; }

        public string DLL { get; set; }
        public string DLLEntryPoint { get; set; }

        [DefaultValue(false)]
        public bool EnableAssemblyVersionCheck { get; set; } = false;

        // changing implicit loading behavior
        [DefaultValue(true)]
        public bool LoadImplicitManifest { get; set; } = true;

        // custom resources types that will be passed into FinishedLoading method
        public HashSet<string> CustomResourceTypes { get; set; } = new();

        // palce for add enum files
        public List<DataAddendumEntry> DataAddendumEntries { get; set; } = new();

        // manifest, for including any kind of things to add to the game's manifest
        public List<ModEntry> Manifest { get; set; } = new();

        // remove these entries by ID from the game
        [Obsolete]
        public List<string> RemoveManifestEntries { get; set; } = new();

        // a settings file to be nice to our users and have a known place for settings
        // these will be different depending on the mod obviously
        public JObject Settings { get; set; } = new();

        [JsonIgnore]
        public bool LoadFail { get; set; } = false;

        [JsonIgnore]
        public bool PendingEnable { get; set; } = false;

        [JsonIgnore]
        public string FailReason { get; set; }

        public void SaveState()
        {
            var modStatePath = Path.Combine(Directory, ModTek.MOD_STATE_JSON_NAME);
            var state = new ModState();
            state.Enabled = Enabled;
            RLog.M.WL(2, "writing to FS:" + Name + "->" + state.Enabled);
            state.SaveToPath(modStatePath);
        }

        /// <summary>
        /// Creates a ModDef from a path to a mod.json
        /// </summary>
        public static ModDefEx CreateFromPath(string path)
        {
            var modDef = JsonConvert.DeserializeObject<ModDefEx>(File.ReadAllText(path));
            modDef.Directory = Path.GetDirectoryName(path);
            modDef.LoadFail = false;
            modDef.FailReason = string.Empty;
            var statepath = Path.Combine(Path.GetDirectoryName(path), ModTek.MOD_STATE_JSON_NAME);
            if (File.Exists(statepath))
            {
                try
                {
                    var stateDef = JsonConvert.DeserializeObject<ModState>(File.ReadAllText(statepath));
                    modDef.Enabled = stateDef.Enabled;
                }
                catch (Exception)
                {
                    var state = new ModState();
                    state.Enabled = modDef.Enabled;
                    state.SaveToPath(statepath);
                }
            }
            else
            {
                var state = new ModState();
                state.Enabled = modDef.Enabled;
                state.SaveToPath(statepath);
            }

            modDef.PendingEnable = modDef.Enabled;
            return modDef;
        }

        public string toJSON()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Checks if all dependencies are present in param loaded
        /// </summary>
        public bool AreDependenciesResolved(IEnumerable<string> loaded)
        {
            return DependsOn.Count == 0 || DependsOn.Intersect(loaded).Count() == DependsOn.Count;
        }

        /// <summary>
        /// Checks against provided list of mods to see if any of them conflict
        /// </summary>
        public bool HasConflicts(IEnumerable<string> otherMods)
        {
            return ConflictsWith.Intersect(otherMods).Any();
        }

        /// <summary>
        /// Checks to see if this ModDef should load, providing a reason if shouldn't load
        /// </summary>
        public bool ShouldTryLoad(List<string> alreadyTryLoadMods, out string reason, out bool shouldAddToList)
        {
            if (!Enabled)
            {
                reason = "it is disabled";
                IgnoreLoadFailure = true;
                shouldAddToList = true;
                return false;
            }

            shouldAddToList = false;
            if (alreadyTryLoadMods.Contains(Name))
            {
                reason = $"ModTek already loaded with the same name. Skipping load from {FileUtils.GetRelativePath(Directory, FilePaths.ModsDirectory)}.";
                return false;
            }

            // check game version vs. specific version or against min/max
            if (!string.IsNullOrEmpty(BattleTechVersion) && !VersionInfo.ProductVersion.StartsWith(BattleTechVersion))
            {
                reason = $"it specifies a game version and this isn't it ({BattleTechVersion} vs. game {VersionInfo.ProductVersion})";
                return false;
            }

            var btgVersion = new Version(VersionInfo.ProductVersion);
            if (!string.IsNullOrEmpty(BattleTechVersionMin) && btgVersion < new Version(BattleTechVersionMin))
            {
                reason = $"it doesn't match the min version set in the mod.json ({BattleTechVersionMin} vs. game {VersionInfo.ProductVersion})";
                return false;
            }

            if (!string.IsNullOrEmpty(BattleTechVersionMax) && btgVersion > new Version(BattleTechVersionMax))
            {
                reason = $"it doesn't match the max version set in the mod.json ({BattleTechVersionMax} vs. game {VersionInfo.ProductVersion})";
                return false;
            }

            reason = "";
            return true;
        }
    }
}
