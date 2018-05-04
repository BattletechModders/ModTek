using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BattleTech;
using BattleTechModLoader;
using Harmony;
using Newtonsoft.Json;

namespace ModTek
{
    public static class Core
    {
        public static string ModDirectory { get; private set; }

        private static string logPath;
        private static string MOD_JSON_NAME = "mod.json";
        
        // ran by BTML
        public static void Init()
        {
            ModDirectory = Path.GetFullPath(BTModLoader.ModDirectory);
            logPath = Path.Combine(ModDirectory, "ModTek.log");

            // init harmony and patch the stuff that comes with ModTek
            var harmony = HarmonyInstance.Create("io.github.mpstark.ModTek");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            using (var logWriter = File.CreateText(logPath))
            {
                var modDefs = new List<ModDef>();
                logWriter.WriteLine("ModTek -- {0}", DateTime.Now);
                
                // find all sub-directories that have a mod.json file
                var modDirectories = Directory.GetDirectories(ModDirectory).Where(x => File.Exists(Path.Combine(x, MOD_JSON_NAME)));
                if (modDirectories.Count() == 0)
                    logWriter.WriteLine("No ModTek-compatable mods found.");
                foreach (var modDirectory in modDirectories)
                {
                    var modDefPath = Path.Combine(modDirectory, MOD_JSON_NAME);
                    var modDef = JsonConvert.DeserializeObject<ModDef>(File.ReadAllText(modDefPath));
                    
                    if (modDef == null)
                    {
                        logWriter.WriteLine("ModDef found in {0} not valid.", modDefPath);
                        continue;
                    }

                    if (modDef.Enabled)
                    {
                        modDef.Directory = Path.GetDirectoryName(modDefPath);
                        modDefs.Add(modDef);
                        logWriter.WriteLine("Loaded ModDef for {0}.", modDef.Name);
                    }
                    else
                    {
                        logWriter.WriteLine("{0} disabled, skipping load.", modDef.Name);
                        continue;
                    }

                    // TODO: build some sort of dependacy graph
                }

                // TODO: load mods in the correct order
                foreach (var modDef in modDefs)
                {
                    Loading.LoadMod(modDef, logWriter);
                }
            }
        }
    }
}
