using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BattleTech;
using BattleTechModLoader;
using Harmony;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            if (!string.IsNullOrEmpty(logPath))
                using (var logWriter = File.AppendText(logPath))
                    logWriter.WriteLine(message, formatObjects);
        }
        internal static void LogWithDate(string message, params object[] formatObjects)
        {
            if (!string.IsNullOrEmpty(logPath))
                using (var logWriter = File.AppendText(logPath))
                    logWriter.WriteLine(DateTime.Now.ToLongTimeString() + " - " + message, formatObjects);
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

            // create ModDef objects for each mod.json file
            var modDefs = new List<ModDef>();
            foreach (var modDirectory in modDirectories)
            {
                var modDefPath = Path.Combine(modDirectory, MOD_JSON_NAME);

                try
                {
                    ModDef modDef = ModDefFromPath(modDefPath);

                    if (!modDef.Enabled)
                    {
                        LogWithDate("Will not load {0} because it's disabled.", modDef.Name);
                        continue;
                    }

                    modDefs.Add(modDef);
                }
                catch (Exception e)
                {
                    Log("Caught exception while parsing {1} at path {0}", modDefPath, MOD_JSON_NAME);
                    Log("Exception: {0}", e.ToString());
                    continue;
                }
            }

            // TODO: be able to read load order from a JSON
            var loadOrder = GetLoadOrder(modDefs, out List<ModDef> willNotLoad);
            while (loadOrder.Count() > 0)
            {
                var modDef = loadOrder.Dequeue();
                LoadMod(modDef);
            }

            foreach (var modDef in willNotLoad)
            {
                LogWithDate("Will not load {0} because its dependancies are unmet.", modDef.Name);
            }
        }

        internal static Queue<ModDef> GetLoadOrder(List<ModDef> modDefs, out List<ModDef> unloaded)
        {
            var loadOrder = new Queue<ModDef>();
            var loaded = new HashSet<string>();
            unloaded = modDefs.OrderByDescending(x => x.Name).ToList();
            
            int removedThisPass;
            do
            {
                removedThisPass = 0;

                for (int i = unloaded.Count - 1; i >= 0; i--)
                {
                    var modDef = unloaded[i];
                    if (modDef.DependsOn == null || modDef.DependsOn.Count == 0 || modDef.DependsOn.Intersect(loaded).Count() == modDef.DependsOn.Count)
                    {
                        unloaded.RemoveAt(i);
                        loadOrder.Enqueue(modDef);
                        loaded.Add(modDef.Name);
                        removedThisPass++;
                    }
                }
            } while (removedThisPass > 0 && unloaded.Count > 0);

            return loadOrder;
        }

        internal static ModDef ModDefFromPath(string path)
        {
            ModDef modDef;
            modDef = JsonConvert.DeserializeObject<ModDef>(File.ReadAllText(path));
            modDef.Directory = Path.GetDirectoryName(path);
            return modDef;
        }

        public static string InferIDFromJObject(JObject jObj, string type = null)
        {
            // go through the different kinds of id storage in JSONS
            // TODO: make this specific to the type
            string[] jPaths = { "Description.Id", "id", "Id", "ID", "identifier", "Identifier" };
            string id;
            foreach (var jPath in jPaths)
            {
                id = (string)jObj.SelectToken(jPath);
                if (id != null)
                    return id;
            }

            return null;
        }
        
        public static string InferIDFromFileAndType(string path, string type)
        {
            if (Path.GetExtension(path).ToLower() == "json" && File.Exists(path))
            {
                try
                {
                    var jObj = JObject.Parse(File.ReadAllText(path));
                    return InferIDFromJObject(jObj, type);
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

                    if (string.IsNullOrEmpty(entry.Path) || string.IsNullOrEmpty(entry.Type))
                    {
                        LogWithDate("{0} has a manifest entry that is missing its type or path! Aborting load.", modDef.Name);
                        return;
                    }
                    
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
                            
                            potentialAdditions.Add(id, new ModDef.ManifestEntry(entry.Type, filePath));
                        }
                    }
                    else if (File.Exists(entryPath))
                    {
                        // path is a file, add the single entry
                        var id = entry.Id;

                        if (id == null)
                        {
                            id = InferIDFromFileAndType(entryPath, entry.Type);
                        }

                        if (potentialAdditions.ContainsKey(id))
                        {
                            LogWithDate("{0}'s manifest has a file at {1}, but it's inferred ID is already used by this mod! Aborting load.", modDef.Name, entryPath);
                            return;
                        }

                        potentialAdditions.Add(id, new ModDef.ManifestEntry(entry.Type, entryPath));
                    }
                    else
                    {
                        // wasn't a file and wasn't a path, must not exist
                        LogWithDate("{0} has a manifest entry {1}, but it's missing! Aborting load.", modDef.Name, entryPath);
                        return;
                    }
                }
            }

            // load mod dll
            if (modDef.DLL != null)
            {
                var dllPath = Path.Combine(modDef.Directory, modDef.DLL);
                string typeName = null;
                string methodName = "Init";

                if (!File.Exists(dllPath))
                {
                    LogWithDate("{0} has a DLL specified ({1}), but it's missing! Aborting load.", modDef.Name, dllPath);
                    return;
                }
                
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

                LogWithDate("Using BTML to load dll {0} with entry path {1}.{2}", Path.GetFileName(dllPath), typeName ?? "NoNameSpecified", methodName);
                BTModLoader.LoadDLL(dllPath, methodName, typeName, new object[] { modDef.Directory, modDef.Settings.ToString(Formatting.None) });
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
