using System;
using System.IO;
using ModTek.Features.Logging;
using Newtonsoft.Json;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Misc
{
    internal class Configuration
    {
        [JsonProperty]
        internal bool ShowLoadingScreenErrors = true;
        internal string ShowLoadingScreenErrors_Description => "TODO";

        [JsonProperty]
        internal bool ShowErrorPopup = true;
        internal string ShowErrorPopup_Description => "TODO";

        [JsonProperty]
        internal bool UseErrorWhiteList = true;
        internal string UseErrorWhiteList_Description => "TODO";

        [JsonProperty]
        internal string[] ErrorWhitelist = { "Data.DataManager [ERROR] ManifestEntry is null" };

        [JsonProperty]
        internal bool UseFileCompression = false; // false for pre v2.0 behavior
        internal string UseFileCompression_Description => "Manifest, database and cache files are compressed using gzip.";

        [JsonProperty]
        internal bool SearchModsInSubDirectories = true; // false for pre v2.0 behavior
        internal string SearchModsInSubDirectories_Description => "Searches recursively all directories for mod.json instead of just the ones under Mods.";

        [JsonProperty]
        internal bool ImplicitManifestShouldMergeJSON = true;
        internal string ImplicitManifestShouldMergeJSON_Description => "How JSONs in a mods implicit manifest (StreamingAssets) are being treated.";

        [JsonProperty]
        internal bool ImplicitManifestShouldAppendText = false; // false for pre v2.0 behavior
        internal string ImplicitManifestShouldAppendText_Description => "How CSVs in a mods implicit manifest (StreamingAssets) are being treated.";

        [JsonProperty]
        internal bool PreloadResourcesForCache = false; // preloading happens as soon as dlc manifest is fully loaded
        internal string PreloadResourcesForCache_Description => "Instead of waiting for the game to requests resources naturally and then merge when loading" +
            ", pre-request all mergeable and indexable resources during the game startup. Not all mods can work with this, therefore disable by default.";

        [JsonProperty]
        internal string[] BlockedMods = { "FYLS" };
        internal string BlockedMods_Description => "Mods that should not be allowed to load, useful in case those interfere with the current version of ModTek.";

        [JsonProperty]
        internal string[] IgnoreMissingMods = { "FYLS" };
        internal string IgnoreMissingMods_Description => "Ignore the dependency requirement of mods that depend on ignored mods. Useful if ModTek takes over the same functionality of the ignored mod.";

        [JsonProperty]
        internal LoggingSettings Logging = new();

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
                            DefaultValueHandling = DefaultValueHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore
                        }
                    );
                    Log($"Loaded config from path: {path}");
                }
                catch (Exception e)
                {
                    Log("Reading configuration failed, using defaults", e);
                }
            }
            else
            {
                File.WriteAllText(path, "{}");
            }

            config.WriteConfig(ConfigLastPath);

            return config;
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
