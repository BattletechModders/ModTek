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

        public static Dictionary<string, ModDef.ManifestEntry> NewManifestEntries { get; set; } = new Dictionary<string, ModDef.ManifestEntry>();

        public static void LogMessage(string message, params object[] formatThings)
        {
            if (logPath != null && logPath != "")
            {
                using (var logWriter = File.AppendText(logPath))
                {
                    logWriter.WriteLine(message, formatThings);
                }
            }
        }

        // ran by BTML
        public static void Init()
        {
            ModDirectory = Path.GetFullPath(BTModLoader.ModDirectory);
            logPath = Path.Combine(ModDirectory, "ModTek.log");

            // init harmony and patch the stuff that comes with ModTek
            var harmony = HarmonyInstance.Create("io.github.mpstark.ModTek");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            

            var modDefs = new List<ModDef>();
            LogMessage("ModTek -- {0}", DateTime.Now);

            // find all sub-directories that have a mod.json file
            var modDirectories = Directory.GetDirectories(ModDirectory).Where(x => File.Exists(Path.Combine(x, MOD_JSON_NAME)));
            if (modDirectories.Count() == 0)
                LogMessage("No ModTek-compatable mods found.");
            foreach (var modDirectory in modDirectories)
            {
                var modDefPath = Path.Combine(modDirectory, MOD_JSON_NAME);
                var modDef = JsonConvert.DeserializeObject<ModDef>(File.ReadAllText(modDefPath));

                if (modDef == null)
                {
                    LogMessage("ModDef found in {0} not valid.", modDefPath);
                    continue;
                }

                if (modDef.Enabled)
                {
                    modDef.Directory = Path.GetDirectoryName(modDefPath);
                    modDefs.Add(modDef);
                    LogMessage("Loaded ModDef for {0}.", modDef.Name);
                }
                else
                {
                    LogMessage("{0} disabled, skipping load.", modDef.Name);
                    continue;
                }

                // TODO: build some sort of dependacy graph
            }

            // TODO: load mods in the correct order
            foreach (var modDef in modDefs)
            {
                LoadMod(modDef);
            }
        }


        private static string InferIDFromFileAndType(string path, string type)
        {
            // this is actually the worst function known to mankind
            // TODO: uhh.. make it not awful
            if (Path.GetExtension(path).ToLower() == "json" && File.Exists(path))
            {
                try
                {
                    string id;
                    var jObj = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
                    
                    id = (string)jObj.SelectToken("Description.Id");
                    if (id != null)
                        return id;

                    id = (string)jObj.SelectToken("id");
                    if (id != null)
                        return id;

                    id = (string)jObj.SelectToken("Id");
                    if (id != null)
                        return id;

                    id = (string)jObj.SelectToken("ID");
                    if (id != null)
                        return id;

                    id = (string)jObj.SelectToken("identifier");
                    if (id != null)
                        return id;

                    id = (string)jObj.SelectToken("Identifier");
                    if (id != null)
                        return id;
                }
                catch { }
            }

            // fall back to using the path
            return Path.GetFileNameWithoutExtension(path);
        }

        public static void LoadMod(ModDef modDef)
        {
            var potentialAdditions = new Dictionary<string, ModDef.ManifestEntry>();

            LogMessage("Loading {0}", modDef.Name);

            // load out of the manifest
            // TODO: actually ignore the modDef specified files/directories
            if (modDef.Manifest != null && modDef.Manifest.Count > 0)
            {
                foreach (var entry in modDef.Manifest)
                {
                    var path = Path.Combine(modDef.Directory, entry.Path);

                    if (entry.Path != null && entry.Path != "" && entry.Type != null && entry.Type != "")
                    {
                        if (Directory.Exists(path))
                        {
                            // path is a directory, add all the files there
                            var files = Directory.GetFiles(path);
                            foreach (var filePath in files)
                            {
                                var id = InferIDFromFileAndType(filePath, entry.Type);

                                if (potentialAdditions.ContainsKey(id))
                                {
                                    LogMessage("{0}'s manifest has a file at {1}, but it's inferred ID is already used by this mod! Aborting load.", modDef.Name, filePath);
                                    return;
                                }
                                else
                                {
                                    potentialAdditions.Add(id, new ModDef.ManifestEntry(entry.Type, filePath));
                                }
                            }
                        }
                        else if (File.Exists(path))
                        {
                            // path is a file, add the single entry
                            var id = entry.ID;

                            if (id == null)
                                id = InferIDFromFileAndType(path, entry.Type);

                            if (potentialAdditions.ContainsKey(id))
                            {
                                LogMessage("{0}'s manifest has a file at {1}, but it's inferred ID is already used by this mod! Aborting load.", modDef.Name, path);
                                return;
                            }
                            else
                            {
                                potentialAdditions.Add(id, new ModDef.ManifestEntry(entry.Type, path));
                            }
                        }
                        else
                        {
                            LogMessage("{0} has a manifest entry {1}, but it's missing! Aborting load.", modDef.Name, path);
                            return;
                        }
                    }
                    else
                    {
                        LogMessage("{0} has a manifest entry that is missing its type or path! Aborting load.", modDef.Name);
                        return;
                    }
                }
            }

            // load mod dll
            if (modDef.DLL != null)
            {
                var dllPath = Path.Combine(modDef.Directory, modDef.DLL);

                if (File.Exists(dllPath))
                {
                    string typeName = null;
                    string methodName = "Init";

                    if (modDef.DLLEntryPoint != null)
                    {
                        int pos = modDef.DLLEntryPoint.LastIndexOf('.');
                        if (pos == -1)
                        {
                            methodName = modDef.DLLEntryPoint;
                        }
                        else
                        {
                            typeName = modDef.DLLEntryPoint.Substring(0, pos - 1);
                            methodName = modDef.DLLEntryPoint.Substring(pos + 1);
                        }
                    }

                    BTModLoader.LoadDLL(dllPath, null, methodName, typeName, new object[] { modDef.Directory, modDef.Settings });
                }
                else
                {
                    LogMessage("{0} has a DLL specified ({1}), but it's missing! Aborting load.", modDef.Name, dllPath);
                    return;
                }
            }
            
            // actually add the additions, since we successfully got through loading the other stuff
            if (potentialAdditions.Count > 0)
            {
                foreach (var additionKVP in potentialAdditions)
                {
                    LogMessage("\tWill load manifest entry -- id: {0} type: {1} path: {2}", additionKVP.Key, additionKVP.Value.Type, additionKVP.Value.Path);
                    NewManifestEntries[additionKVP.Key] = additionKVP.Value;
                }
            }

            LogMessage("Loaded {0}", modDef.Name);
        }
    }
}
