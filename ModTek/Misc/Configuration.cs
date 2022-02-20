using System;
using System.IO;
using ModTek.Features.Logging;
using ModTek.Features.Profiling;
using Newtonsoft.Json;

namespace ModTek.Misc
{
    internal class Configuration
    {
        [JsonProperty]
        internal bool ShowLoadingScreenErrors = true;
        [JsonProperty]
        internal const string ShowLoadingScreenErrors_Description = "TODO";

        [JsonProperty]
        internal bool ShowErrorPopup = true;
        [JsonProperty]
        internal const string ShowErrorPopup_Description = "TODO";

        [JsonProperty]
        internal bool UseErrorWhiteList = true;
        [JsonProperty]
        internal const string UseErrorWhiteList_Description = "TODO";

        [JsonProperty]
        internal string[] ErrorWhitelist = { "Data.DataManager [ERROR] ManifestEntry is null" };

        [JsonProperty]
        internal bool SearchModsInSubDirectories = true;
        [JsonProperty]
        internal const string SearchModsInSubDirectories_Description = "Searches recursively all directories for mod.json instead only for directories directly found under Mods. Set to false for pre v2.0 behavior.";

        [JsonProperty]
        internal bool ImplicitManifestShouldMergeJSON = true;
        [JsonProperty]
        internal const string ImplicitManifestShouldMergeJSON_Description = "How JSONs in a mods implicit manifest (StreamingAssets) are being treated.";

        [JsonProperty]
        internal bool ImplicitManifestShouldAppendText;
        [JsonProperty]
        internal const string ImplicitManifestShouldAppendText_Description = "How CSVs in a mods implicit manifest (StreamingAssets) are being treated.";

        [JsonProperty]
        internal bool NormalizeCsvIfAppending = true;
        [JsonProperty]
        internal const string NormalizeCsvIfAppending_Description = "Normalize CSV files when merging/appending. Filters out empty lines and adds newlines where appropiate. Duplicate title detection and removal from appending files.";

        [JsonProperty]
        internal float DataManagerUnfreezeDelta = 2f;
        [JsonProperty]
        internal readonly string DataManagerUnfreezeDelta_Description = $"How often to refresh the UI during loading. Does this by skipping loads every specified amount of seconds.";

        [JsonProperty]
        internal float DataManagerEverSpinnyDetectionTimespan = 30f;
        [JsonProperty]
        internal readonly string DataManagerEverSpinnyDetectionTimespan_Description = $"How long data is not being further processed until it is assumed to be stuck for good. Upon detection it dumps lots of data into the log, but nothing more.";

        [JsonProperty]
        internal bool DelayPrewarmToMainMenu = true;
        [JsonProperty]
        internal const string DelayPrewarmToMainMenu_Description = "Delays executing prewarm requests until entering the main menu and dlc packs are loaded. Prevents choppy intro video and makes sure to include dlc items during prewarm.";

        [JsonProperty]
        internal bool ShowDataManagerStatsInLoadingCurtain = true;
        [JsonProperty]
        internal const string ShowDataManagerStatsInLoadingCurtain_Description = "Adds DataManager stats when showing a loading curtain.";

        [JsonProperty]
        internal string[] BlockedMods = { "FYLS" };
        [JsonProperty]
        internal const string BlockedMods_Description = "Mods that should not be allowed to load. Useful in cases where those mods would (newly) interfere with ModTek.";

        [JsonProperty]
        internal string[] IgnoreMissingMods = { "FYLS" };
        [JsonProperty]
        internal const string IgnoreMissingMods_Description = "Ignore the dependency requirement of mods that depend on the listed mods. Useful if e.g. ModTek provides the same functionality as the ignored mods.";

        [JsonProperty]
        internal string[] AssembliesToPreload = { };
        [JsonProperty]
        internal const string AssembliesToPreload_Description = "A list of assemblies to preload before ModTek starts harmony patching." +
            " Useful for mods that modify the assembly directly and introduce dependencies not found in the default assembly search path of the game." +
            " Path is relative to the Mods/ directory";

        [JsonProperty]
        internal LoggingSettings Logging = new LoggingSettings();

        [JsonProperty]
        internal ProfilingSettings Profiling = new ProfilingSettings();

        [JsonIgnore]
        private Exception ReadConfigurationException;

        private static string ConfigPath => Path.Combine(FilePaths.ModTekDirectory, "config.json");
        private static string ConfigDefaultsPath => Path.Combine(FilePaths.ModTekDirectory, "config.defaults.json");
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
                    MTLogger.Info.Log($"Loaded config from path: {path}");
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

            config.WriteConfig(ConfigLastPath);

            return config;
        }

        internal void LogAnyDanglingExceptions()
        {
            if (ReadConfigurationException != null)
            {
                MTLogger.Warning.Log("Reading configuration failed, using defaults", ReadConfigurationException);
            }
        }

        private void WriteConfig(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this,
                Formatting.Indented
            ));
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}
