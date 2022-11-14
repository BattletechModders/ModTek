using System;
using System.IO;
using System.Reflection;
using ModTek.Features.Logging;
using ModTek.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace ModTek.Misc
{
    internal class Configuration
    {
        [JsonProperty]
        internal readonly string _Description = $"When changing any of the listed settings, copy the relevant parts into `{FileUtils.GetRelativePath(ConfigPath)}`.";

        [JsonProperty]
        internal const string ShowLoadingScreenErrors_Description = "TODO";
        [JsonProperty]
        internal bool ShowLoadingScreenErrors = true;

        [JsonProperty]
        internal const string ShowErrorPopup_Description = "TODO";
        [JsonProperty]
        internal bool ShowErrorPopup = true;

        [JsonProperty]
        internal const string UseErrorWhiteList_Description = "TODO";
        [JsonProperty]
        internal bool UseErrorWhiteList = true;
        [JsonProperty]
        internal string[] ErrorWhitelist = { "Data.DataManager [ERROR] ManifestEntry is null" };

        [JsonProperty]
        internal const string SearchModsInSubDirectories_Description = "Searches recursively all directories for mod.json instead only for directories directly found under Mods. Set to false for pre v2.0 behavior.";
        [JsonProperty]
        internal bool SearchModsInSubDirectories = true;

        [JsonProperty]
        internal const string ImplicitManifestShouldMergeJSON_Description = "How JSONs in a mods implicit manifest (StreamingAssets) are being treated.";
        [JsonProperty]
        internal bool ImplicitManifestShouldMergeJSON = true;

        [JsonProperty]
        internal const string ImplicitManifestShouldAppendText_Description = "How CSVs in a mods implicit manifest (StreamingAssets) are being treated.";
        [JsonProperty]
        internal bool ImplicitManifestShouldAppendText;

        [JsonProperty]
        internal const string ReplaceResetsMerges_Description = "If enabled, once a manifest entry gets replaced, all previously queued merges will not apply.";
        [JsonProperty]
        internal bool ReplaceResetsMerges = true;

        [JsonProperty]
        internal const string NormalizeCsvIfAppending_Description = "Normalize CSV files when merging/appending. Filters out empty lines and adds newlines where appropiate. Duplicate title detection and removal from appending files.";
        [JsonProperty]
        internal bool NormalizeCsvIfAppending = true;

        [JsonProperty]
        internal readonly string DataManagerUnfreezeDelta_Description = $"How often to refresh the UI during loading. Does this by skipping loads every specified amount of seconds.";
        [JsonProperty]
        internal float DataManagerUnfreezeDelta = 2f;

        [JsonProperty]
        internal readonly string DataManagerEverSpinnyDetectionTimespan_Description = $"How long data is not being further processed until it is assumed to be stuck for good. Upon detection it dumps lots of data into the log, but nothing more.";
        [JsonProperty]
        internal float DataManagerEverSpinnyDetectionTimespan = 30f;

        [JsonProperty]
        internal const string DelayPrewarmToMainMenu_Description = "Delays executing prewarm requests until entering the main menu and dlc packs are loaded. Prevents choppy intro video and makes sure to include dlc items during prewarm.";
        [JsonProperty]
        internal bool DelayPrewarmToMainMenu = true;

        [JsonProperty]
        internal const string ShowDataManagerStatsInLoadingCurtain_Description = "Adds DataManager stats when showing a loading curtain.";
        [JsonProperty]
        internal bool ShowDataManagerStatsInLoadingCurtain = true;

        [JsonProperty]
        internal const string BlockedMods_Description = "Mods that should not be allowed to load. Useful in cases where those mods would (newly) interfere with ModTek.";
        [JsonProperty]
        internal string[] BlockedMods = { "FYLS" };

        [JsonProperty]
        internal const string IgnoreMissingMods_Description = "Ignore the dependency requirement of mods that depend on the listed mods. Useful if e.g. ModTek provides the same functionality as the ignored mods.";
        [JsonProperty]
        internal string[] IgnoreMissingMods = { "FYLS" };

        [JsonProperty]
        internal const string MinimalLastConfig_Description = "Hides all null and _Description fields from the generated `last` config file.";
        [JsonProperty]
        internal bool MinimalLastConfig = true;

        [JsonProperty]
        internal LoggingSettings Logging = new LoggingSettings();

        [JsonIgnore]
        private Exception ReadConfigurationException;

        private static string ConfigPath => Path.Combine(FilePaths.ModTekDirectory, "config.json");
        private static string ConfigDefaultsPath => Path.Combine(FilePaths.ModTekDirectory, "config.help.json");
        private static string ConfigLastPath => Path.Combine(FilePaths.ModTekDirectory, "config.last.json");

        internal static Configuration FromDefaultFile()
        {
            var path = ConfigPath;
            var config = new Configuration();
            config.WriteConfig(ConfigDefaultsPath);

            if (File.Exists(path))
            {
                try
                {
                    var text = File.ReadAllText(path);
                    JsonConvert.PopulateObject(
                        text,
                        config,
                        new JsonSerializerSettings
                        {
                            ObjectCreationHandling = ObjectCreationHandling.Replace,
                            NullValueHandling = NullValueHandling.Ignore
                        }
                    );
                }
                catch (Exception e)
                {
                    config.ReadConfigurationException = e;
                }
            }
            else
            {
                File.WriteAllText(path, "{}");
            }

            config.WriteConfig(ConfigLastPath, config.MinimalLastConfig);

            return config;
        }

        internal void LogAnyDanglingExceptions()
        {
            if (ReadConfigurationException != null)
            {
                Log.Main.Warning?.Log("Reading configuration failed, using defaults", ReadConfigurationException);
            }
        }

        private void WriteConfig(string path, bool minimal = false)
        {
            using (var sw = new StreamWriter(path))
            using (var jw = new JsonTextWriter(sw))
            {
                jw.Formatting = Formatting.Indented;
                jw.IndentChar = ' ';
                jw.Indentation = 4;
                var serializer = new JsonSerializer
                {
                    NullValueHandling = minimal ? NullValueHandling.Ignore : NullValueHandling.Include
                };
                if (minimal)
                {
                    serializer.ContractResolver = ShouldSerializeContractResolver.Instance;
                }
                serializer.Converters.Add(new StringEnumConverter());
                serializer.Serialize(jw, this);
            }
        }

        private class ShouldSerializeContractResolver : DefaultContractResolver
        {
            public static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                property.ShouldSerialize = _ => !property.PropertyName.EndsWith("_Description");
                return property;
            }
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}
