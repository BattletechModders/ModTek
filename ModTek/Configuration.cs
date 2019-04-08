using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using HBS.Logging;
using ModTek.Logging;
using Logger = ModTek.Util.Logger;

namespace ModTek
{
    public class Configuration
    {
        public bool ShowLoadingScreenErrors = true;
        public bool ShowErrorPopup = true;
        public bool UseErrorWhiteList = true;
        public List<string> ErrorWhitelist = new List<string> { "Data.DataManager [ERROR] ManifestEntry is null" };
        public BetterLogSettings CleanedLogSettings = new BetterLogSettings { LogFileEnabled = true, LogLevel = LogLevel.Log };
        public bool EnableStackTraceLogging = false;

        public void ToFile(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static Configuration FromFile(string path)
        {
            if (!File.Exists(path))
            {
                Logger.Log("Building new config.");
                return new Configuration();
            }

            try
            {
                var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(path),
                    new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
                Logger.Log($"Loaded config.");

                return config;
            }
            catch (Exception e)
            {
                Logger.LogException("Reading configuration failed -- will rebuild it!", e);
                return new Configuration();
            }
        }
    }
}
