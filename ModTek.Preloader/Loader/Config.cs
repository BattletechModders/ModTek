﻿using System;
using System.IO;
using ModTek.Common.Globals;
using ModTek.Common.Utils;
using Newtonsoft.Json;

namespace ModTek.Preloader.Loader;

internal class Config
{
#pragma warning disable CS0649
    [JsonProperty]
    internal readonly string _Description = $"When changing any of the listed settings, copy the relevant parts into `{FileUtils.GetRelativePath(Paths.PreloaderConfigFile)}`.";

    [JsonProperty]
    internal readonly string Harmony12XLogChannelFilter_Description
        = $"The channels to log into `{FileUtils.GetRelativePath(Paths.HarmonyLogFile)}`: None=0 Info=2 IL=4 Warn=8 Error=16 Debug=32 All=62";
    [JsonProperty]
    internal int Harmony12XLogChannelFilter = 26;

    [JsonProperty]
    internal readonly string Harmony12XFakeAssemblyLocationEnabled_Description =
        "Make Assembly.Location return the path of the original non-shimmed assembly and not the path to the shimmed assembly. Workaround to some mods expecting their assembly to be in their respective mod directory.";
    [JsonProperty]
    internal bool Harmony12XFakeAssemblyLocationEnabled = true;
#pragma warning restore CS0649

    internal static Config Instance = new();

    private Config()
    {
        FileUtils.CreateDirectoryForFile(Paths.PreloaderConfigDefaultsFile);
        File.WriteAllText(
            Paths.PreloaderConfigDefaultsFile,
            JsonConvert.SerializeObject(this, Formatting.Indented)
        );

        if (File.Exists(Paths.PreloaderConfigFile))
        {
            try
            {
                var text = File.ReadAllText(Paths.PreloaderConfigFile);
                JsonConvert.PopulateObject(
                    text,
                    this,
                    new JsonSerializerSettings
                    {
                        ObjectCreationHandling = ObjectCreationHandling.Replace,
                        NullValueHandling = NullValueHandling.Ignore,
                    }
                );
            }
            catch (Exception e)
            {
                Logger.Main.Log($"Could not read config at {Paths.PreloaderConfigFile}: {e}");
            }
        }
        else
        {
            File.WriteAllText(Paths.PreloaderConfigFile, "{}");
        }
    }
}