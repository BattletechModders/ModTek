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
        internal bool EnableDebugLogging = true;

        [JsonProperty]
        internal bool UseFileCompression = true;

        [JsonProperty]
        internal string[] BlockedMods = { "FYLS" };

        [JsonProperty]
        internal LoggingSettings Logging = new();

        internal static Configuration FromDefaultFile()
        {
            var path = FilePaths.ConfigPath;
            var config = new Configuration();
            config.WriteDefaultConfig();

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

            Log($"Configuration: {config}");
            return config;
        }

        private void WriteDefaultConfig()
        {
            File.WriteAllText(FilePaths.ConfigDefaultsPath, JsonConvert.SerializeObject(this,
                Formatting.Indented
            ));
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}
