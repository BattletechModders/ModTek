using System;
using System.IO;
using ModTekPreloader.Logging;
using Newtonsoft.Json;

namespace ModTekPreloader.Loader
{
    internal class Config
    {
#pragma warning disable CS0649
        [JsonProperty]
        internal readonly string _Description = $"When changing any of the listed settings, copy the relevant parts into `{Paths.GetRelativePath(Paths.PreloaderConfigFile)}`.";

        [JsonProperty]
        internal readonly string AssembliesToMakePublic_Description =
            $"All listed will be copied to `{Paths.GetRelativePath(Paths.AssembliesPublicizedDirectory)}`." +
            " The copies will have all their members (classes, methods, properties and fields) made public." +
            " It should be used to compile against, see the README for more information.";

        [JsonProperty]
        internal string[] AssembliesToMakePublic =
        {
            "Assembly-CSharp",
            "Assembly-CSharp-firstpass",
            "BattleTech.Common"
        };

        [JsonProperty]
        internal readonly string TypesToNotMakePublic_Description =
            "Add full names of types not to make public, in case you want to subclass these types and visibility changes would throw errors or crash the game.";

        [JsonProperty]
        internal string[] TypesToNotMakePublic =
        {
        };

        [JsonProperty]
        internal readonly string Harmony12XEnabled_Description =
            "Enables Harmony X and its shims for Harmony 1 and 2, does work pretty well but not perfectly and some mods might need to be updated.";

        [JsonProperty]
        internal bool Harmony12XEnabled;
#pragma warning restore CS0649

        internal static Config Instance = new Config();

        private Config()
        {
            Paths.CreateDirectoryForFile(Paths.PreloaderConfigDefaultsFile);
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
                    Logger.Log($"Could not read config at {Paths.PreloaderConfigFile}: {e}");
                }
            }
            else
            {
                File.WriteAllText(Paths.PreloaderConfigFile, "{}");
            }
        }
    }
}
