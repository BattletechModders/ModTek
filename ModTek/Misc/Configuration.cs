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

        [JsonProperty]
        internal bool ShowErrorPopup = true;

        [JsonProperty]
        internal bool UseErrorWhiteList = true;

        [JsonProperty]
        internal string[] ErrorWhitelist = { "Data.DataManager [ERROR] ManifestEntry is null" };

        [JsonProperty]
        internal bool UseFileCompression = false; // false for pre v2.0 behavior

        [JsonProperty]
        internal bool SearchModsInSubDirectories = true; // false for pre v2.0 behavior

        [JsonProperty]
        internal bool ImplicitManifestShouldMergeJSON = true;

        [JsonProperty]
        internal bool ImplicitManifestShouldAppendText = false; // false for pre v2.0 behavior

        [JsonProperty]
        internal bool PreloadResourcesForCache = false; // preloading happens as soon as dlc manifest is fully loaded

        [JsonProperty]
        internal string[] BlockedMods = { "FYLS" };

        [JsonProperty]
        internal string[] IgnoreMissingMods = { "FYLS" };

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
