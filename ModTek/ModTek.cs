using BattleTech;
using BattleTechModLoader;
using Harmony;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
        // ReSharper disable once MemberCanBePrivate.Global
        public static string ModDirectory { get; private set; }

        private const string MOD_JSON_NAME = "mod.json";

        // ReSharper disable once BuiltInTypeReferenceStyle
        private static Dictionary<Int32, string> JsonHashToId { get; } =
            new Dictionary<int, string>();

        private static Dictionary<string, List<string>> JsonMerges { get; } =
            new Dictionary<string, List<string>>();

        private static Dictionary<string, ModDef.ManifestEntry> NewManifestEntries { get; } =
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
                LoadMod(modDef);
            }

            foreach (var modDef in willNotLoad)
            {
                LogWithDate($"Will not load {modDef} because its dependancies are unmet.");
            }
        }

        private static void PropagateConflictsForward(Dictionary<string, ModDef> modDefs)
        {
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

        internal static Queue<ModDef> GetLoadOrder(Dictionary<string, ModDef> modDefs, out List<string> skippedMods)
        {
            var loadOrder = new Queue<ModDef>();
            var loadedMods = new HashSet<string>();
            var candidateMods = new SortedDictionary<string, ModDef>(modDefs);

            //// remove all mods that can't ever be loaded due to missing dependency
            // not necessary as load order issues have to be fixed by the user anyway
            // only helps with allowing conflicting mods to load in case the offender can't be loaded due to dependency issues
            //candidateMods.Values.ToList() // iterate over copy (ToList) to allow removal in original
            //    .Where(m => !m.DependsOn.All(modDefs.ContainsKey))
            //    .Do(m => candidateMods.Remove(m.Name));

            // remove all mods who have conflicts, in order to be fair, remove all mods that conflict and don't pick a winner
            candidateMods.Values.ToList() // iterate over copy (ToList) to allow removeal in original
                .Where(m => m.ConflictsWith.Any(modDefs.ContainsKey))
                .Do(m => candidateMods.Remove(m.Name));

            int candidateCountBefore;
            do
            {
                candidateCountBefore = candidateMods.Count;

                candidateMods.Values.ToList() // iterate over copy (ToList) to allow removal in original
                    .Where(m => m.DependsOn.All(loadedMods.Contains))
                    .Do(m =>
                    {
                        loadOrder.Enqueue(m);
                        loadedMods.Add(m.Name);
                        candidateMods.Remove(m.Name);
                    });
            } while (candidateMods.Any() && candidateMods.Count != candidateCountBefore);

            skippedMods = modDefs.Keys.Except(loadedMods).ToList();

            return loadOrder;
        }

        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        // ReSharper disable once MemberCanBePrivate.Global
        internal static ModDef ModDefFromPath(string path)
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
                return InferIDFromJObject(jObj, type);
            }
            catch (Exception e)
            {
                Log($"\tException occurred while parsing type {type} json at {path}: {e}");
            }

            // fall back to using the path
            return Path.GetFileNameWithoutExtension(path);
        }

        public static void TryMergeJsonInto(string jsonIn, ref string jsonOut)
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
                Log($"Error merging JSON ${e}");
            }
        }

        public static void TryAddToVersionManifest(VersionManifest manifest)
        {
            foreach (var entryKvp in NewManifestEntries)
            {
                var id = entryKvp.Key;
                var newEntry = entryKvp.Value;

                if (newEntry.ShouldMergeJSON && manifest.Contains(id, newEntry.Type))
                {
                    // read the manifest pointed entry and hash the contents
                    JsonHashToId.Add(File.ReadAllText(manifest.Get(id, newEntry.Type).FilePath).GetHashCode(), id);

                    // The manifest already contains this information, so we need to queue it to be merged
                    var partialJson = File.ReadAllText(newEntry.Path);

                    if (!JsonMerges.ContainsKey(id))
                        JsonMerges.Add(id, new List<string>());

                    Log($"\tAdding id {id} to JSONMerges");
                    JsonMerges[id].Add(partialJson);
                }
                else
                {
                    // This is a new definition or a replacement that doesn't get merged, so add or update the manifest
                    Log($"\tAddOrUpdate({id}, {newEntry.Path}, {newEntry.Type}, {DateTime.Now}, {newEntry.AssetBundleName}, {newEntry.AssetBundlePersistent})");
                    manifest.AddOrUpdate(id, newEntry.Path, newEntry.Type, DateTime.Now, newEntry.AssetBundleName, newEntry.AssetBundlePersistent);
                }
            }
        }

        [UsedImplicitly]
        public static void LoadMod(ModDef modDef)
        {
            var potentialAdditions = new Dictionary<string, ModDef.ManifestEntry>();

            LogWithDate($"Loading {modDef.Name}");

            // load out of the manifest
            // TODO: actually ignore the modDef specified files/directories
            if (modDef.Manifest != null && modDef.Manifest.Count > 0)
            {
                foreach (var entry in modDef.Manifest)
                {
                    var entryPath = Path.Combine(modDef.Directory, entry.Path);

                    if (string.IsNullOrEmpty(entry.Path) || string.IsNullOrEmpty(entry.Type))
                    {
                        LogWithDate($"{modDef.Name} has a manifest entry that is missing its type or path! Aborting load.");
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
                                LogWithDate($"{modDef.Name}'s manifest has a file at {filePath}, but it's inferred ID is already used by this mod! Aborting load.");
                                return;
                            }

                            potentialAdditions.Add(id, new ModDef.ManifestEntry(entry.Type, filePath));
                        }
                    }
                    else if (File.Exists(entryPath))
                    {
                        // path is a file, add the single entry
                        var id = entry.Id ?? InferIDFromFileAndType(entryPath, entry.Type);

                        if (potentialAdditions.ContainsKey(id))
                        {
                            LogWithDate($"{modDef.Name}'s manifest has a file at {entryPath}, but it's inferred ID is already used by this mod! Aborting load.");
                            return;
                        }

                        potentialAdditions.Add(id, new ModDef.ManifestEntry(entry.Type, entryPath));
                    }
                    else
                    {
                        // wasn't a file and wasn't a path, must not exist
                        LogWithDate($"{modDef.Name} has a manifest entry {entryPath}, but it's missing! Aborting load.");
                        return;
                    }
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
                    LogWithDate($"{modDef.Name} has a DLL specified ({dllPath}), but it's missing! Aborting load.");
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

                LogWithDate($"Using BTML to load dll {Path.GetFileName(dllPath)} with entry path {typeName ?? "NoNameSpecified"}.{methodName}");

                BTModLoader.LoadDLL(dllPath, methodName, typeName,
                    new object[] { modDef.Directory, modDef.Settings.ToString(Formatting.None) });
            }

            // actually add the additions, since we successfully got through loading the other stuff
            if (potentialAdditions.Count > 0)
            {
                // TODO, this kvp is the weirdest thing
                foreach (var additionKvp in potentialAdditions)
                {
                    Log($"\tWill load manifest entry -- id: {additionKvp.Key} type: {additionKvp.Value.Type} path: {additionKvp.Value.Path}");
                    NewManifestEntries[additionKvp.Key] = additionKvp.Value;
                }
            }

            LogWithDate($"Loaded {modDef.Name}");
        }
    }
}
