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
using System.Text.RegularExpressions;
using BattleTech.Data;
using UnityEngine.Assertions;

namespace ModTek
{
    using static Logger;

    [UsedImplicitly]
    public static class ModTek
    {
        [UsedImplicitly]
        public static string ModDirectory { get; private set; }

        [UsedImplicitly]
        public static string StreamingAssetsDirectory { get; private set; }

        private const string MOD_JSON_NAME = "mod.json";

        private static bool hasLoadedMods = false;
        private static List<string> modLoadOrder;

        private static Dictionary<Int32, string> JsonHashToId { get; } =
            new Dictionary<int, string>();

        private static Dictionary<string, List<string>> JsonMerges { get; } =
            new Dictionary<string, List<string>>();
        
        private static Dictionary<string, List<ModDef.ManifestEntry>> ModManifest { get; } =
            new Dictionary<string, List<ModDef.ManifestEntry>>();

        // ran by BTML
        [UsedImplicitly]
        public static void Init()
        {
            var manifestDirectory = Path.GetDirectoryName(VersionManifestUtilities.MANIFEST_FILEPATH);

            Assert.IsNotNull(manifestDirectory, nameof(manifestDirectory) + " != null");

            ModDirectory = Path.GetFullPath(
                Path.Combine(manifestDirectory,
                    Path.Combine(Path.Combine(Path.Combine(
                                 "..", ".."), ".."), "Mods")));
            StreamingAssetsDirectory = Path.GetFullPath(Path.Combine(manifestDirectory, ".."));
            LogPath = Path.Combine(ModDirectory, "ModTek.log");

            // create log file, overwritting if it's already there
            using (var logWriter = File.CreateText(LogPath))
                logWriter.WriteLine($"ModTek v{Assembly.GetExecutingAssembly().GetName().Version} -- {DateTime.Now}");

            // init harmony and patch the stuff that comes with ModTek (contained in Patches.cs)
            var harmony = HarmonyInstance.Create("io.github.mpstark.ModTek");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [UsedImplicitly]
        public static void LoadMod(ModDef modDef)
        {
            var potentialAdditions = new List<ModDef.ManifestEntry>();

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
                        var childModDef = new ModDef.ManifestEntry(entry, filePath, InferIDFromFileAndType(filePath, entry.Type));
                        potentialAdditions.Add(childModDef);
                    }
                }
                else if (File.Exists(entryPath))
                {
                    // path is a file, add the single entry
                    entry.Id = entry.Id ?? InferIDFromFileAndType(entryPath, entry.Type);
                    entry.Path = entryPath;
                    potentialAdditions.Add(entry);
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
                        typeName = modDef.DLLEntryPoint.Substring(0, pos);
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
                foreach (var addition in potentialAdditions)
                {
                    Log($"\tNew Entry: {addition.Path.Replace(ModDirectory, "")}");
                }

                ModManifest[modDef.Name] = potentialAdditions;
            }
        }

        internal static void LoadMods()
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

                    if (modDefs.ContainsKey(modDef.Name))
                    {
                        LogWithDate($"Already loaded a mod named {modDef.Name}. Skipping load from {modDef.Directory}.");
                        continue;
                    }

                    modDefs.Add(modDef.Name, modDef);
                }
                catch (Exception e)
                {
                    Log($"Caught exception while parsing {MOD_JSON_NAME} at path {modDefPath}");
                    Log($"\t{e.Message}");
                    //continue;
                }
            }

            // TODO: be able to read load order from a JSON
            PropagateConflictsForward(modDefs);
            modLoadOrder = GetLoadOrder(modDefs, out var willNotLoad);

            // lists guarentee order
            foreach (var modName in modLoadOrder)
            {
                var modDef = modDefs[modName];

                try
                {
                    LoadMod(modDef);
                }
                catch (Exception e)
                {
                    LogWithDate($"Exception caught while trying to load {modDef.Name}");
                    Log($"{e.Message}");
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

        private static List<string> GetLoadOrder(Dictionary<string, ModDef> modDefs, out List<string> unloaded)
        {
            var loadOrder = new List<string>();
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
                    loadOrder.Add(modDef.Name);
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
                Log($"\tException occurred while parsing type {type} json at {path}");
                Log($"\t\t{e.Message}");
            }

            // fall back to using the path
            return Path.GetFileNameWithoutExtension(path);
        }

        internal static string FixMissingCommas(string json)
        {
            var rgx = new Regex(@"(\]|\}|""|[A-Za-z0-9])\s*\n\s*(\[|\{|"")", RegexOptions.Singleline);
            return rgx.Replace(json, "$1,\n$2");
        }

        internal static void TryMergeIntoInterceptedJson(string jsonIn, ref string jsonOut)
        {
            var jsonHash = jsonIn.GetHashCode();
            var jsonCopy = jsonOut;

            if (!JsonHashToId.ContainsKey(jsonHash))
                return;

            var id = JsonHashToId[jsonHash];

            if (!JsonMerges.ContainsKey(id))
                return;

            LogWithDate($"Merging json into ID: {id}");
            
            JObject ontoJObj;
            try
            {
                ontoJObj = JObject.Parse(jsonCopy);
            }
            catch (Exception e)
            {
                try
                {
                    Log("\tParent JSON has an JSON parse error, attempting to fix missing commas with regex");
                    jsonCopy = FixMissingCommas(jsonCopy);
                    ontoJObj = JObject.Parse(jsonCopy);
                }
                catch (Exception e2)
                {
                    Log("\tParent JSON has an error preventing merges that couldn't be fixed with missing comma regex");
                    Log($"\t\t Exception before regex: {e.Message}");
                    Log($"\t\t Exception after regex: {e2.Message}");
                    return;
                }

                Log("\tFixed missing commas in parent JSON.");
            }

            foreach (var jsonMerge in JsonMerges[id])
            {
                try
                {
                    var inJObj = JObject.Parse(jsonMerge);
                    ontoJObj.Merge(inJObj, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                }
                catch (Exception e)
                {
                    Log($"\tError merging particular JSON merge onto {id}, skipping just this single merge");
                    Log($"\t\t{e.Message}");
                }
            }

            jsonOut = ontoJObj.ToString();
        }

        internal static void TryAddToVersionManifest(VersionManifest manifest)
        {
            if (!hasLoadedMods)
                LoadMods();

            var breakMyGame = File.Exists(Path.Combine(ModDirectory, "break.my.game"));
            
            LogWithDate("Adding in mod manifests!");

            if (breakMyGame)
            {
                var mddPath = Path.Combine(Path.Combine(StreamingAssetsDirectory, "MDD"), "MetadataDatabase.db");
                var mddBackupPath = mddPath + ".orig";

                Log($"\tBreak my game mode enabled! All new modded content (doesn't currently support merges) will be added to the DB.");
                
                if (!File.Exists(mddBackupPath))
                {
                    Log($"\t\tBacking up metadata database to {Path.GetFileName(mddBackupPath)}");
                    File.Copy(mddPath, mddBackupPath);
                }
            }

            foreach (var modName in modLoadOrder)
            {
                if (!ModManifest.ContainsKey(modName))
                    continue;

                Log($"\t{modName}:");
                foreach (var modEntry in ModManifest[modName])
                {
                    var existingEntry = manifest.Find(x => x.Id == modEntry.Id);
                    VersionManifestAddendum addendum = null;

                    if (!string.IsNullOrEmpty(modEntry.AddToAddendum))
                    {
                        addendum = manifest.GetAddendumByName(modEntry.AddToAddendum);

                        // create the addendum if it doesn't exist
                        if (addendum == null)
                        {
                            Log($"\t\tCreated addendum {modEntry.AddToAddendum}:");
                            addendum = new VersionManifestAddendum(modEntry.AddToAddendum);
                            manifest.ApplyAddendum(addendum);
                        }
                    }

                    if (modEntry.Type == null)
                    {
                        // null type means that we have to find existing entry with the same rel path to fill in the entry
                        // TODO: + 16 is a little bizzare looking, it's the length of the substring + 1 because we want to get rid of it and the \
                        var relPath = modEntry.Path.Substring(modEntry.Path.LastIndexOf("StreamingAssets", StringComparison.Ordinal) + 16);
                        var fakeStreamingAssetsPath = Path.Combine(StreamingAssetsDirectory, relPath);

                        existingEntry = manifest.Find(x => Path.GetFullPath(x.FilePath) == Path.GetFullPath(fakeStreamingAssetsPath));

                        if (existingEntry == null)
                            continue;

                        modEntry.Id = existingEntry.Id;
                        modEntry.Type = existingEntry.Type;
                    }

                    if (Path.GetExtension(modEntry.Path).ToLower() == ".json" && modEntry.ShouldMergeJSON && existingEntry != null)
                    {
                        // read the manifest pointed entry and hash the contents
                        JsonHashToId[File.ReadAllText(existingEntry.FilePath).GetHashCode()] = modEntry.Id;

                        // The manifest already contains this information, so we need to queue it to be merged
                        var partialJson = File.ReadAllText(modEntry.Path);

                        if (!JsonMerges.ContainsKey(modEntry.Id))
                            JsonMerges.Add(modEntry.Id, new List<string>());

                        if (JsonMerges[modEntry.Id].Contains(partialJson))
                        {
                            Log($"\t\tAlready added {modEntry.Id} to JsonMerges");
                            continue;
                        }

                        Log($"\t\tAdding {modEntry.Id} to JsonMerges");
                        JsonMerges[modEntry.Id].Add(partialJson);
                        continue;
                    }
                    
                    if (breakMyGame && Path.GetExtension(modEntry.Path).ToLower() == ".json")
                    {
                        var type = (BattleTechResourceType) Enum.Parse(typeof(BattleTechResourceType), modEntry.Type);
                        using (var metadataDatabase = new MetadataDatabase())
                        {
                            VersionManifestHotReload.InstantiateResourceAndUpdateMDDB(type, modEntry.Path, metadataDatabase);
                            Log($"\t\tAdding to MDDB! {type} {modEntry.Path}");
                        }
                    }

                    if (!string.IsNullOrEmpty(modEntry.AddToAddendum))
                    {
                        Log($"\t\tAddOrUpdate {modEntry.Type} {modEntry.Id} to addendum {addendum.Name}");
                        addendum.AddOrUpdate(modEntry.Id, modEntry.Path, modEntry.Type, DateTime.Now, modEntry.AssetBundleName, modEntry.AssetBundlePersistent);
                        continue;
                    }

                    // This is a new definition or a replacement that doesn't get merged, so add or update the manifest
                    Log($"\t\tAddOrUpdate {modEntry.Type} {modEntry.Id}");
                    manifest.AddOrUpdate(modEntry.Id, modEntry.Path, modEntry.Type, DateTime.Now, modEntry.AssetBundleName, modEntry.AssetBundlePersistent);
                }
            }

            Log("");
        }
    }
}
