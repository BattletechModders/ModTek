using System;
using System.IO;
using ModTekPreloader.Logging;
using Newtonsoft.Json;

namespace ModTekPreloader.Loader
{
    internal class Config
    {
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
                            NullValueHandling = NullValueHandling.Ignore
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

        public readonly string AssembliesToMakePublic_Description =
            $"All listed will be copied to {Paths.GetRelativePath(Paths.AssembliesPublicizedDirectory)}." +
            " The copies will have all their members (classes, methods, properties and fields) made public." +
            " It should be used to compile against, see the README for more information.";
        public string[] AssembliesToMakePublic =
        {
            "Assembly-CSharp",
            "Assembly-CSharp-firstpass",
            "BattleTech.Common"
        };

        public readonly string TypesToNotMakePublic_Description =
            "Add full names of types not to make public, in case you want to subclass these types and visibility changes would throw errors or crash the game.";
        public string[] TypesToNotMakePublic =
        {
        };
    }
}
