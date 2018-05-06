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
    public static class ModTek
    {
        public static string ModDirectory { get; private set; }

        private static readonly string MOD_JSON_NAME = "mod.json";

        internal static Dictionary<string, ModDef.ManifestEntry> NewManifestEntries { get; set; } = new Dictionary<string, ModDef.ManifestEntry>();
        
        // logging
        internal static string logPath;
        internal static void Log(string message, params object[] formatObjects)
        {
            if (logPath != null && logPath != "")
                using (var logWriter = File.AppendText(logPath))
                    logWriter.WriteLine(message, formatObjects);
        }
        internal static void LogWithDate(string message, params object[] formatObjects)
        {
            if (logPath != null && logPath != "")
                using (var logWriter = File.AppendText(logPath))
                    logWriter.WriteLine(DateTime.Now.ToShortTimeString() + " - " + message, formatObjects);
        }

        // ran by BTML
        public static void Init()
        {
            ModDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(VersionManifestUtilities.MANIFEST_FILEPATH), @"..\..\..\Mods\"));
            logPath = Path.Combine(ModDirectory, "ModTek.log");

            // create log file, overwritting if it's already there
            using (var logWriter = File.CreateText(logPath))
                logWriter.WriteLine("ModTek -- {0}", DateTime.Now);
            
            // init harmony and patch the stuff that comes with ModTek (contained in Patches.cs)
            var harmony = HarmonyInstance.Create("io.github.mpstark.ModTek");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // find all sub-directories that have a mod.json file
            var modDirectories = Directory.GetDirectories(ModDirectory).Where(x => File.Exists(Path.Combine(x, MOD_JSON_NAME)));
            if (modDirectories.Count() == 0)
                Log("No ModTek-compatable mods found.");

            // create ModDef objects for each mod.json file, building a dependancy graph
            var modDefs = new List<ModDef>();
            foreach (var modDirectory in modDirectories)
            {
                var modDefPath = Path.Combine(modDirectory, MOD_JSON_NAME);
                var modDef = JsonConvert.DeserializeObject<ModDef>(File.ReadAllText(modDefPath));

                if (modDef == null)
                {
                    Log("ModDef found in {0} not valid.", modDefPath);
                    continue;
                }

                if (modDef.Enabled)
                {
                    modDef.Directory = Path.GetDirectoryName(modDefPath);
                    modDefs.Add(modDef);
                    Log("Loaded ModDef for {0}.", modDef.Name);
                }
                else
                {
                    Log("{0} disabled, skipping load.", modDef.Name);
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
        
        internal static string InferIDFromFileAndType(string path, string type)
        {
            if (Path.GetExtension(path).ToLower() == "json" && File.Exists(path))
            {
                try
                {
                    var jObj = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));
                    string id;
                    
                    // go through the different kinds of id storage in JSONS
                    // TODO: make this specific to the type
                    string[] jPaths = { "Description.Id", "id", "Id", "ID", "identifier", "Identifier" };
                    foreach (var jPath in jPaths)
                    {
                        id = (string)jObj.SelectToken(jPath);
                        if (id != null)
                            return id;
                    }
                }
                catch (Exception e)
                {
                    Log("\tException occurred while parsing type {1} json at {0}: {2}", path, type, e.ToString());
                }
            }

            // fall back to using the path
            return Path.GetFileNameWithoutExtension(path);
        }

        public static void LoadMod(ModDef modDef)
        {
            var potentialAdditions = new Dictionary<string, ModDef.ManifestEntry>();

            LogWithDate("Loading {0}", modDef.Name);

            // load out of the manifest
            // TODO: actually ignore the modDef specified files/directories
            if (modDef.Manifest != null && modDef.Manifest.Count > 0)
            {
                foreach (var entry in modDef.Manifest)
                {
                    var entryPath = Path.Combine(modDef.Directory, entry.Path);

                    if (entry.Path != null && entry.Path != "" && entry.Type != null && entry.Type != "")
                    {
                        if (Directory.Exists(entryPath))
                        {
                            // path is a directory, add all the files there
                            var files = Directory.GetFiles(entryPath);
                            foreach (var filePath in files)
                            {
                                var id = InferIDFromFileAndType(filePath, entry.Type);

                                if (potentialAdditions.ContainsKey(id))
                                {
                                    LogWithDate("{0}'s manifest has a file at {1}, but it's inferred ID is already used by this mod! Aborting load.", modDef.Name, filePath);
                                    return;
                                }
                                else
                                {
                                    potentialAdditions.Add(id, new ModDef.ManifestEntry(entry.Type, filePath));
                                }
                            }
                        }
                        else if (File.Exists(entryPath))
                        {
                            // path is a file, add the single entry
                            var id = entry.ID;

                            if (id == null)
                                id = InferIDFromFileAndType(entryPath, entry.Type);

                            if (potentialAdditions.ContainsKey(id))
                            {
                                LogWithDate("{0}'s manifest has a file at {1}, but it's inferred ID is already used by this mod! Aborting load.", modDef.Name, entryPath);
                                return;
                            }
                            else
                            {
                                potentialAdditions.Add(id, new ModDef.ManifestEntry(entry.Type, entryPath));
                            }
                        }
                        else
                        {
                            LogWithDate("{0} has a manifest entry {1}, but it's missing! Aborting load.", modDef.Name, entryPath);
                            return;
                        }
                    }
                    else
                    {
                        LogWithDate("{0} has a manifest entry that is missing its type or path! Aborting load.", modDef.Name);
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

                    LogWithDate("Using BTML to load dll {0} with entry path {1}.{2}", Path.GetFileName(dllPath), (typeName != null)?typeName:"NoNameSpecified", methodName);
                    BTModLoader.LoadDLL(dllPath, null, methodName, typeName, new object[] { modDef.Directory, modDef.Settings });
                }
                else
                {
                    LogWithDate("{0} has a DLL specified ({1}), but it's missing! Aborting load.", modDef.Name, dllPath);
                    return;
                }
            }
            
            // actually add the additions, since we successfully got through loading the other stuff
            if (potentialAdditions.Count > 0)
            {
                // TODO, this kvp is the weirdest thing
                foreach (var additionKVP in potentialAdditions)
                {
                    Log("\tWill load manifest entry -- id: {0} type: {1} path: {2}", additionKVP.Key, additionKVP.Value.Type, additionKVP.Value.Path);
                    NewManifestEntries[additionKVP.Key] = additionKVP.Value;
                }
            }

            LogWithDate("Loaded {0}", modDef.Name);
        }
    }
}
