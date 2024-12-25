using System;
using System.IO;
using System.Text.RegularExpressions;
using ModTek.Common.Globals;
using ModTek.Common.Utils;

namespace ModTek.Preloader.Loader;

internal class Config
{
    internal readonly string _Description = $"When changing any of the listed settings, copy the relevant parts into `{FileUtils.GetRelativePath(Paths.PreloaderConfigFile)}`. This is not a normal JSON format, each key-value pairs have to be on separate lines.";
    internal readonly string Harmony12XLogChannelFilter_Description
        = $"The channels to log into `{FileUtils.GetRelativePath(Paths.HarmonyLogFile)}`: None=0 Info=2 IL=4 Warn=8 Error=16 Debug=32 All=62";
    internal int Harmony12XLogChannelFilter = 26;
    internal readonly string Harmony12XFakeAssemblyLocationEnabled_Description =
        "Make Assembly.Location return the path of the original non-shimmed assembly and not the path to the shimmed assembly. Workaround to some mods expecting their assembly to be in their respective mod directory.";
    internal bool Harmony12XFakeAssemblyLocationEnabled = true;

    internal static Config Instance = new();

    private Config()
    {
        FileUtils.CreateDirectoryForFile(Paths.PreloaderConfigDefaultsFile);
        File.WriteAllText(
            Paths.PreloaderConfigDefaultsFile,
            $$"""
              {
                "_Description": "{{_Description}}",
                "Harmony12XLogChannelFilter_Description": "{{Harmony12XLogChannelFilter_Description}}",
                "Harmony12XLogChannelFilter": {{Harmony12XLogChannelFilter}},
                "Harmony12XFakeAssemblyLocationEnabled_Description": "{{Harmony12XFakeAssemblyLocationEnabled_Description}}",
                "Harmony12XFakeAssemblyLocationEnabled": {{Harmony12XFakeAssemblyLocationEnabled.ToString().ToLowerInvariant()}}
              }
              """
        );

        if (File.Exists(Paths.PreloaderConfigFile))
        {
            try
            {
                var text = File.ReadAllText(Paths.PreloaderConfigFile);
                // avoid preloading a JSON library, so let's do some Regex instead
                var regex = new Regex("""^\s*"([^"]+?)"\s*:\s*(.+?)\s*,?\s*$""");
                foreach (Match match in regex.Matches(text))
                {
                    var key = match.Groups[0].Value;
                    var value = match.Groups[1].Value;
                    switch (key)
                    {
                        case "Harmony12XLogChannelFilter":
                            Harmony12XLogChannelFilter = int.Parse(value);
                            break;
                        case "Harmony12XFakeAssemblyLocationEnabled":
                            Harmony12XFakeAssemblyLocationEnabled = bool.Parse(value);
                            break;
                    }
                }
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