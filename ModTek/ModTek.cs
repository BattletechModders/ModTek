using BattleTech;
using BattleTechModLoader;
using Harmony;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine.Assertions;

namespace ModTek
{
    using static Logger;

    [UsedImplicitly]
    public static class ModTek
    {
        [UsedImplicitly]
        public static string ModDirectory { get; private set; }

        private const string MOD_JSON_NAME = "mod.json";
        private static bool hasLoadedMods = false;

        private static Dictionary<Int32, string> JsonHashToId { get; } =
            new Dictionary<int, string>();

        private static Dictionary<string, List<string>> JsonMerges { get; } =
            new Dictionary<string, List<string>>();
        
        private static Dictionary<string, ModDef.ManifestEntry> ModManifest { get; } =
            new Dictionary<string, ModDef.ManifestEntry>();

        // ran by BTML
        [UsedImplicitly]
        public static void Init()
        {
            var manifestDirectory = Path.GetDirectoryName(VersionManifestUtilities.MANIFEST_FILEPATH);

            Assert.IsNotNull(manifestDirectory, nameof(manifestDirectory) + " != null");

            ModDirectory = Path.GetFullPath(Path.Combine(manifestDirectory, @"..\..\..\Mods\"));
            LogPath = Path.Combine(ModDirectory, "ModTek.log");

            // create log file, overwritting if it's already there
            using (var logWriter = File.CreateText(LogPath))
                logWriter.WriteLine($"ModTek -- {DateTime.Now}");

            // init harmony and patch the stuff that comes with ModTek (contained in Patches.cs)
            var harmony = HarmonyInstance.Create("io.github.mpstark.ModTek");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [UsedImplicitly]
        public static void LoadMod(ModDef modDef)
        {
            var potentialAdditions = new Dictionary<string, ModDef.ManifestEntry>();

            LogWithDate($"Loading {modDef.Name}");

            // load out of the manifest
            if (modDef.LoadImplicitManifest && modDef.Manifest.All(x => Path.GetFullPath(Path.Combine(modDef.Directory, x.Path)) != Path.GetFullPath(Path.Combine(modDef.Directory, "StreamingAssets"))))
                modDef.Manifest.Add(new ModDef.ManifestEntry("StreamingAssets", true));

            foreach (var entry in modDef.Manifest)
            {
                var entryPath = Path.Combine(modDef.Directory, entry.Path);

                if (string.IsNullOrEmpty(entry.Path) && (string.IsNullOrEmpty(entry.Type) || entry.Path == "StreamingAssets"))
                {
                    Log($"\t{modDef.Name} has a manifest entry that is missing its path! Aborting load.");
                    return;
                }

                if (Directory.Exists(entryPath))
                {
                    // path is a directory, add all the files there
                    var files = Directory.GetFiles(entryPath, "*", SearchOption.AllDirectories);
                    foreach (var filePath in files)
                    {
                        var id = InferIDFromFileAndType(filePath, entry.Type);
                        var childModDef = new ModDef.ManifestEntry(entry, filePath, id);

                        if (potentialAdditions.ContainsKey(id))
                        {
                            Log($"\t{modDef.Name}'s manifest has a file at {filePath}, but it's inferred ID is already used by this mod! Aborting load.");
                            return;
                        }

                        potentialAdditions.Add(id, childModDef);
                    }
                }
                else if (File.Exists(entryPath))
                {
                    // path is a file, add the single entry
                    entry.Id = entry.Id ?? InferIDFromFileAndType(entryPath, entry.Type);
                    entry.Path = entryPath;

                    if (potentialAdditions.ContainsKey(entry.Id))
                    {
                        Log($"\t{modDef.Name}'s manifest has a file at {entryPath}, but it's inferred ID is already used by this mod! Aborting load.");
                        return;
                    }

                    potentialAdditions.Add(entry.Id, entry);
                }
                else
                {
                    // wasn't a file and wasn't a path, must not exist

                    // TODO: what to do with manifest entries that aren't implicit that are missing?

                    //Log($"\t{modDef.Name} has a manifest entry {entryPath}, but it's missing! Aborting load.");
                    //return;
                }
            }

            // load mod dll
            if (modDef.DLL != null)
            {
                var dllPath = Path.Combine(modDef.Directory, modDef.DLL);
                string typeName = null;
                var methodName = "Init";

                if (!File.Exists(dllPath))
                {
                    Log($"\t{modDef.Name} has a DLL specified ({dllPath}), but it's missing! Aborting load.");
                    return;
                }

                if (modDef.DLLEntryPoint != null)
                {
                    var pos = modDef.DLLEntryPoint.LastIndexOf('.');
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

                Log($"\tUsing BTML to load dll {Path.GetFileName(dllPath)} with entry path {typeName ?? "NoNameSpecified"}.{methodName}");

                BTModLoader.LoadDLL(dllPath, methodName, typeName,
                    new object[] { modDef.Directory, modDef.Settings.ToString(Formatting.None) });
            }

            // actually add the additions, since we successfully got through loading the other stuff
            if (potentialAdditions.Count > 0)
            {
                foreach (var additionKvp in potentialAdditions)
                {
                    var id = additionKvp.Key;
                    var entry = additionKvp.Value;
                    var relativeModPath = entry.Path.Replace(ModDirectory, "");

                    Log($"\tNew Entry: {relativeModPath}");
                    ModManifest[id] = entry;
                }
            }

            LogWithDate($"Loaded {modDef.Name}");
        }

        private static void LoadMods()
        {
            if (hasLoadedMods)
                return;

            // find all sub-directories that have a mod.json file
            var modDirectories = Directory.GetDirectories(ModDirectory)
                .Where(x => File.Exists(Path.Combine(x, MOD_JSON_NAME))).ToArray();
            if (modDirectories.Length == 0)
                Log("No ModTek-compatable mods found.");

            // create ModDef objects for each mod.json file
            var modDefs = new Dictionary<string, ModDef>();
            foreach (var modDirectory in modDirectories)
            {
                var modDefPath = Path.Combine(modDirectory, MOD_JSON_NAME);

                try
                {
                    var modDef = ModDefFromPath(modDefPath);

                    if (!modDef.Enabled)
                    {
                        LogWithDate($"Will not load {modDef.Name} because it's disabled.");
                        continue;
                    }

                    modDefs.Add(modDef.Name, modDef);
                }
                catch (Exception e)
                {
                    Log($"Caught exception while parsing {MOD_JSON_NAME} at path {modDefPath}");
                    Log($"Exception: {e}");
                    //continue;
                }
            }

            // TODO: be able to read load order from a JSON
            PropagateConflictsForward(modDefs);
            var loadOrder = GetLoadOrder(modDefs, out var willNotLoad);
            while (loadOrder.Count > 0)
            {
                var modDef = loadOrder.Dequeue();

                try
                {
                    LoadMod(modDef);
                }
                catch (Exception e)
                {
                    LogWithDate($"Exception caught while trying to load {modDef.Name}: {e}");
                }
            }

            foreach (var modDef in willNotLoad)
            {
                LogWithDate($"Will not load {modDef}. It's lacking a dependancy or a conflict loaded before it.");
            }

            Log("");
            Log("----------");
            Log("");
            hasLoadedMods = true;
        }

        private static void PropagateConflictsForward(Dictionary<string, ModDef> modDefs)
        {
            // conflicts are a unidirectional edge, so make them one in ModDefs
            foreach (var modDefKvp in modDefs)
            {
                var modDef = modDefKvp.Value;
                if (modDef.ConflictsWith.Count == 0) continue;

                foreach (var conflict in modDef.ConflictsWith)
                {
                    modDefs[conflict].ConflictsWith.Add(modDef.Name);
                }
            }
        }

        private static Queue<ModDef> GetLoadOrder(Dictionary<string, ModDef> modDefs, out List<string> unloaded)
        {
            var loadOrder = new Queue<ModDef>();
            var loaded = new HashSet<string>();
            unloaded = modDefs.Keys.OrderByDescending(x => x).ToList();

            int removedThisPass;
            do
            {
                removedThisPass = 0;

                for (var i = unloaded.Count - 1; i >= 0; i--)
                {
                    var modDef = modDefs[unloaded[i]];
                    if (modDef.DependsOn.Count != 0 && modDef.DependsOn.Intersect(loaded).Count() != modDef.DependsOn.Count
                        || modDef.ConflictsWith.Count != 0 && modDef.ConflictsWith.Intersect(loaded).Any()) continue;

                    unloaded.RemoveAt(i);
                    loadOrder.Enqueue(modDef);
                    loaded.Add(modDef.Name);
                    removedThisPass++;
                }
            } while (removedThisPass > 0 && unloaded.Count > 0);

            return loadOrder;
        }
        
        private static ModDef ModDefFromPath(string path)
        {
            var modDef = JsonConvert.DeserializeObject<ModDef>(File.ReadAllText(path));
            modDef.Directory = Path.GetDirectoryName(path);
            return modDef;
        }

        // ReSharper disable once UnusedParameter.Local
        private static string InferIDFromJObject(JObject jObj, string type = null)
        {
            // go through the different kinds of id storage in JSONS
            // TODO: make this specific to the type, remove Resharper disable once above
            string[] jPaths = { "Description.Id", "id", "Id", "ID", "identifier", "Identifier" };
            foreach (var jPath in jPaths)
            {
                var id = (string) jObj.SelectToken(jPath);
                if (id != null)
                    return id;
            }

            return null;
        }

        private static string InferIDFromFileAndType(string path, string type)
        {
            var ext = Path.GetExtension(path);

            if (ext == null || ext.ToLower() != ".json" || !File.Exists(path))
                return Path.GetFileNameWithoutExtension(path);

            try
            {
                var jObj = JObject.Parse(File.ReadAllText(path));
                var id = InferIDFromJObject(jObj, type);

                if (id != null)
                    return id;
            }
            catch (Exception e)
            {
                Log($"\tException occurred while parsing type {type} json at {path}: {e}");
            }

            // fall back to using the path
            return Path.GetFileNameWithoutExtension(path);
        }

        public static void TryMergeIntoInterceptedJson(string jsonIn, ref string jsonOut)
        {
            var jsonHash = jsonIn.GetHashCode();
            var jsonCopy = jsonOut;

            if (!JsonHashToId.ContainsKey(jsonHash))
                return;

            var id = JsonHashToId[jsonHash];

            if (!JsonMerges.ContainsKey(id))
                return;

            try
            {
                var ontoJObj = JObject.Parse(jsonCopy);
                foreach (var jsonMerge in JsonMerges[id])
                {
                    var inJObj = JObject.Parse(jsonMerge);
                    ontoJObj.Merge(inJObj);
                }

                jsonOut = ontoJObj.ToString();
            }
            catch (JsonReaderException e)
            {
                Log($"Error merging JSON! Skipping all mod merges on hash {jsonHash}: {e}");
            }
        }
        
        public static void TryAddToVersionManifest(VersionManifest manifest)
        {
            if (!hasLoadedMods)
                LoadMods();

            LogWithDate("Adding mod manifests to a manifest");
        
            foreach (var entryKvp in ModManifest)
            {
                var id = entryKvp.Key;
                var entry = entryKvp.Value;
                var realEntry = manifest.Find(x => x.Id == id);

                if (entry.Type == null)
                {
                    entry.Type = realEntry?.Type;
                }
                
                if (Path.GetExtension(entry.Path).ToLower() == ".json" && entry.ShouldMergeJSON && realEntry != null)
                {
                    // read the manifest pointed entry and hash the contents
                    JsonHashToId[File.ReadAllText(realEntry.FilePath).GetHashCode()] = id;

                    // The manifest already contains this information, so we need to queue it to be merged
                    var partialJson = File.ReadAllText(entry.Path);

                    if (!JsonMerges.ContainsKey(id))
                        JsonMerges.Add(id, new List<string>());

                    Log($"\tAdding id {id} to JSONMerges");
                    JsonMerges[id].Add(partialJson);
                }
                else
                {
                    // This is a new definition or a replacement that doesn't get merged, so add or update the manifest
                    Log($"\tAddOrUpdate({id}, {entry.Path}, {entry.Type}, {DateTime.Now}, {entry.AssetBundleName}, {entry.AssetBundlePersistent})");
                    manifest.AddOrUpdate(id, entry.Path, entry.Type, DateTime.Now, entry.AssetBundleName, entry.AssetBundlePersistent);
                }
            }
        }
    }
}
