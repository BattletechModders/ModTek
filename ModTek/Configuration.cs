using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace ModTek
{
    public class Configuration
    {
        public bool ShowLoadingScreenErrors = true;
        public bool ShowErrorPopup = true;
        public bool UseErrorWhiteList = true;
        public List<string> ErrorWhitelist = new List<string> { "Data.DataManager [ERROR] ManifestEntry is null" };

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
