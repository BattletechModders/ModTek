using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTek.Features.Manifest.Mods;
using ModTek.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable once CheckNamespace
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ModTek;

// TODO probably exposing too much as public, go through usages in RT
public class ModDefEx : IEquatable<ModDefEx>
{
    // this path will be set at runtime by ModTek
    [JsonIgnore]
    public string Directory { get; set; }

    internal string GetFullPath(string subPath)
    {
        return Path.GetFullPath(Path.Combine(Directory, subPath));
    }

    [JsonProperty(Required = Required.Always)]
    public string Name { get; set; }
    internal string QuotedName => '"' + Name + '"';

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
    public class RequestAtBattleStart
    {
        public BattleTech.BattleTechResourceType Type { get; set; } = BattleTech.BattleTechResourceType.Prefab;
        public string Id { get; set; } = string.Empty;
        public override int GetHashCode()
        {
            return Id.GetHashCode()+Type.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if(obj is RequestAtBattleStart b)
            {
                return Type == b.Type && Id == b.Id;
            }
            return false;
        }
    }
    public HashSet<RequestAtBattleStart> requestAtBattleStarts { get; set; } = new();
    public HashSet<string> forceEnableMods { get; set; } = new();
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
    [Obsolete] // MDD is preloaded, you can't remove them!
    public List<string> RemoveManifestEntries { get; set; } = new();

    // a settings file to be nice to our users and have a known place for settings
    // these will be different depending on the mod obviously
    public JObject Settings { get; set; } = new();

    [JsonIgnore]
    public bool LoadFail { get; set; }

    [JsonIgnore]
    public bool PendingEnable { get; set; }

    [JsonIgnore]
    public string FailReason { get; set; }

    internal void SaveState()
    {
        var modStatePath = Path.Combine(Directory, ModTek.MOD_STATE_JSON_NAME);
        var state = new ModState();
        state.Enabled = Enabled;
        Log.Main.Info?.Log("\t\twriting to FS:" + QuotedName + "->" + state.Enabled);
        state.SaveToPath(modStatePath);
    }

    /// <summary>
    /// Creates a ModDef from a path to a mod.json
    /// </summary>
    internal static ModDefEx CreateFromPath(string path)
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
        modDef.DependsOn.ExceptWith(ModTek.Config.IgnoreMissingMods);
        modDef.OptionallyDependsOn.ExceptWith(ModTek.Config.IgnoreMissingMods);
        return modDef;
    }

    /// <summary>
    /// Checks if all dependencies are present in param loaded
    /// </summary>
    internal List<string> CalcMissingDependsOn(IEnumerable<string> loaded)
    {
        return DependsOn.Except(loaded).ToList();
    }

    /// <summary>
    /// Checks against provided list of mods to see if any of them conflict
    /// </summary>
    internal List<string> CalcConflicts(IEnumerable<string> otherMods)
    {
        return ConflictsWith.Intersect(otherMods).ToList();
    }

    /// <summary>
    /// Checks to see if this ModDef should load, providing a reason if shouldn't load
    /// </summary>
    internal bool ShouldTryLoad(List<string> alreadyTryLoadMods, out string reason, out bool shouldAddToList)
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
            reason = $"ModTek already loaded with the same name. Skipping load from {FileUtils.GetRelativePath(Directory)}.";
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

    // methods generated by JetBrains

    public bool Equals(ModDefEx other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ModDefEx)obj);
    }

    public override int GetHashCode()
    {
        return Name != null ? Name.GetHashCode() : 0;
    }
}