using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using ModTek.Util;

namespace ModTek
{
    public class Configuration
    {
        public bool ShowLoadingScreenErrors = true;
        public bool ShowErrorPopup = true;
        public bool UseErrorWhiteList = true;
        public List<string> ErrorWhitelist = new List<string> { "Data.DataManager [ERROR] ManifestEntry is null" };
        public bool EnableDebugLogging = true;

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
                var text = File.ReadAllText(path);
                var config = JsonConvert.DeserializeObject<Configuration>(text,
                    new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
                Logger.Log($"Loaded config from path: {path}");
                return config;
            }
            catch (Exception e)
            {
                Logger.LogException("Reading configuration failed -- will rebuild it!", e);
                return new Configuration();
            }
        }

        public override string ToString()
        {
            return $"ShowLoadingScreenErrors: {this.ShowLoadingScreenErrors}  ShowErrorPopup: {this.ShowErrorPopup}  " +
                $"EnableDebugLogging: {this.EnableDebugLogging}  UseErrorWhiteList: {this.UseErrorWhiteList}  " +
                $"ErrorWhiteList: {String.Join("','", this.ErrorWhitelist)}";
        }
    }
}
