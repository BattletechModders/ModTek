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
using BattleTech.Data;

namespace ModTek
{
    using static Logger;
    
    public static class ModTek
    {
        public static string GameDirectory { get; private set; }
        public static string ModDirectory { get; private set; }
        public static string StreamingAssetsDirectory { get; private set; }

        internal static string ModTekDirectory { get; private set; }
        internal static string CacheDirectory { get; private set; }
        internal static string MergeCachePath { get; private set; }
        internal static string TypeCachePath { get; private set; }

        internal static Dictionary<string, string> ModAssetBundlePaths { get; } = new Dictionary<string, string>();

        private const string MODS_DIRECTORY_NAME = "Mods";
        private const string MOD_JSON_NAME = "mod.json";
        private const string MODTEK_DIRECTORY_NAME = ".modtek";
        private const string CACHE_DIRECTORY_NAME = "Cache";
        private const string MERGE_CACHE_FILE_NAME = "merge_cache.json";
        private const string TYPE_CACHE_FILE_NAME = "type_cache.json";
        private const string LOG_NAME = "ModTek.log";
        private const string LOAD_ORDER_FILE_NAME = "load_order.json";
        
        private static bool hasLoadedMods; //defaults to false

        private static List<string> modLoadOrder;
        private static MergeCache JsonMergeCache;
        private static Dictionary<string, List<string>> TypeCache;
        
        private static Dictionary<string, List<ModDef.ManifestEntry>> ModManifest = new Dictionary<string, List<ModDef.ManifestEntry>>();
        private static List<ModDef.ManifestEntry> modEntries;

        // ran by BTML
        [UsedImplicitly]
        public static void Init()
        {
            var manifestDirectory = Path.GetDirectoryName(VersionManifestUtilities.MANIFEST_FILEPATH);

            // if the manifest directory is null, there is something seriously wrong
            if (manifestDirectory == null)
                return;
            
            ModDirectory = Path.GetFullPath(
                Path.Combine(manifestDirectory,
                    Path.Combine(Path.Combine(Path.Combine(
                        "..", ".."), ".."), MODS_DIRECTORY_NAME)));

            StreamingAssetsDirectory = Path.GetFullPath(Path.Combine(manifestDirectory, ".."));
            GameDirectory = Path.GetFullPath(Path.Combine(Path.Combine(StreamingAssetsDirectory, ".."), ".."));

            ModTekDirectory = Path.Combine(ModDirectory, MODTEK_DIRECTORY_NAME);
            LogPath = Path.Combine(ModTekDirectory, LOG_NAME);
            CacheDirectory = Path.Combine(ModTekDirectory, CACHE_DIRECTORY_NAME);
            MergeCachePath = Path.Combine(CacheDirectory, MERGE_CACHE_FILE_NAME);
            TypeCachePath = Path.Combine(CacheDirectory, TYPE_CACHE_FILE_NAME);

            // creates the directories above it as well
            Directory.CreateDirectory(CacheDirectory);

            // create log file, overwritting if it's already there
            using (var logWriter = File.CreateText(LogPath))
                logWriter.WriteLine($"ModTek v{Assembly.GetExecutingAssembly().GetName().Version} -- {DateTime.Now}");

            // init harmony and patch the stuff that comes with ModTek (contained in Patches.cs)
            var harmony = HarmonyInstance.Create("io.github.mpstark.ModTek");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // load merge cache if it exists
            if (File.Exists(MergeCachePath))
            {
                try
                {
                    JsonMergeCache = JsonConvert.DeserializeObject<MergeCache>(File.ReadAllText(MergeCachePath));
                    Log("Loaded merge cache.");
                }
                catch (Exception e)
                {
                    JsonMergeCache = new MergeCache();
                    Log("Loading merge cache failed -- will rebuild it.");
                    Log($"\t{e.Message}");
                }
            }
            else
            {
                JsonMergeCache = new MergeCache();
            }

            // load type cache if it exists
            if (File.Exists(TypeCachePath))
            {
                try
                {
                    TypeCache = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(TypeCachePath));
                    Log("Loaded type cache.");
                }
                catch (Exception e)
                {
                    TypeCache = new Dictionary<string, List<string>>();
                    Log("Loading type cache failed -- will rebuild it.");
                    Log($"\t{e.Message}");
                }
            }
            else
            {
                TypeCache = new Dictionary<string, List<string>>();
            }
        }
        
        private static void LoadMod(ModDef modDef)
        {
            var potentialAdditions = new List<ModDef.ManifestEntry>();

            LogWithDate($"Loading {modDef.Name}");

            // load out of the manifest
            if (modDef.LoadImplicitManifest && modDef.Manifest.All(x => Path.GetFullPath(Path.Combine(modDef.Directory, x.Path)) != Path.GetFullPath(Path.Combine(modDef.Directory, "StreamingAssets"))))
                modDef.Manifest.Add(new ModDef.ManifestEntry("StreamingAssets", true));

            foreach (var entry in modDef.Manifest)
            {
                // handle prefabs; they have potential internal path to assetbundle
                if (entry.Type == "Prefab" && !string.IsNullOrEmpty(entry.AssetBundleName))
                {
                    if (!potentialAdditions.Any(x => x.Type == "AssetBundle" && x.Id == entry.AssetBundleName))
                    {
                        Log($"\t{modDef.Name} has a Prefab that's referencing an AssetBundle that hasn't been loaded. Put the assetbundle first in the manifest!");
                        return;
                    }

                    entry.Id = Path.GetFileNameWithoutExtension(entry.Path);
                    potentialAdditions.Add(entry);
                    continue;
                }
                
                if (string.IsNullOrEmpty(entry.Path) && string.IsNullOrEmpty(entry.Type) && entry.Path != "StreamingAssets")
                {
                    Log($"\t{modDef.Name} has a manifest entry that is missing its path or type! Aborting load.");
                    return;
                }

                var entryPath = Path.Combine(modDef.Directory, entry.Path);
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
            {
                Log("No ModTek-compatable mods found.");
                return;
            }

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
                    LogWithDate($"Tried to load mod: {modDef.Name}, but something went wrong. Make sure all of your JSON is correct!");
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

            // write out load order
            File.WriteAllText(Path.Combine(ModTekDirectory, LOAD_ORDER_FILE_NAME), JsonConvert.SerializeObject(modLoadOrder, Formatting.Indented));

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
                Log($"\tCould not parse {path.Replace(ModDirectory, "")} with type {type}. Does this JSON have errors?");
                Log($"\t\t{e.Message}");
            }

            // fall back to using the path
            return Path.GetFileNameWithoutExtension(path);
        }

        private static bool AddModEntryToVersionManifest(VersionManifest manifest, ModDef.ManifestEntry modEntry, bool addToDB = false)
        {
            if (modEntry.Path == null)
                return false;

            VersionManifestAddendum addendum = null;
            if (!string.IsNullOrEmpty(modEntry.AddToAddendum))
            {
                addendum = manifest.GetAddendumByName(modEntry.AddToAddendum);

                // create the addendum if it doesn't exist
                if (addendum == null)
                {
                    Log($"\t\tCreated addendum: {modEntry.AddToAddendum}");
                    addendum = new VersionManifestAddendum(modEntry.AddToAddendum);
                    manifest.ApplyAddendum(addendum);
                }
            }

            // add to DB
            if (addToDB && Path.GetExtension(modEntry.Path).ToLower() == ".json")
            {
                var type = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), modEntry.Type);
                using (var metadataDatabase = new MetadataDatabase())
                {
                    VersionManifestHotReload.InstantiateResourceAndUpdateMDDB(type, modEntry.Path, metadataDatabase);
                    Log($"\t\tAdding to MDDB! {type} {modEntry.Path}");
                }
            }

            // add assetbundle path so it can be changed when the assetbundle path is requested
            if (modEntry.Type == "AssetBundle")
                ModAssetBundlePaths[modEntry.Id] = modEntry.Path;
            
            // add to addendum instead of adding to manifest
            if (addendum != null)
            {
                Log($"\t\tAddOrUpdate => {modEntry.Id} ({modEntry.Type}) to addendum {addendum.Name}");
                addendum.AddOrUpdate(modEntry.Id, modEntry.Path, modEntry.Type, DateTime.Now, modEntry.AssetBundleName, modEntry.AssetBundlePersistent);
                return true;
            }

            // not added to addendum, not added to jsonmerges
            Log($"\t\tAddOrUpdate => {modEntry.Id} ({modEntry.Type})");
            manifest.AddOrUpdate(modEntry.Id, modEntry.Path, modEntry.Type, DateTime.Now, modEntry.AssetBundleName, modEntry.AssetBundlePersistent);
            return true;
        }

        internal static void TryAddToVersionManifest(VersionManifest manifest)
        {
            if (!hasLoadedMods)
                LoadMods();
            
            if (modEntries != null)
            {
                LogWithDate("Loading another manifest with already setup mod manifests.");
                foreach (var modEntry in modEntries)
                {
                    AddModEntryToVersionManifest(manifest, modEntry);
                }
                LogWithDate("Done.");
                return;
            }

            modEntries = new List<ModDef.ManifestEntry>();

            LogWithDate("Setting up mod manifests...");

            var breakMyGame = File.Exists(Path.Combine(ModDirectory, "break.my.game"));
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

            var jsonMerges = new Dictionary<string, List<string>>();

            foreach (var modName in modLoadOrder)
            {
                if (!ModManifest.ContainsKey(modName))
                    continue;
                
                Log($"\t{modName}:");
                foreach (var modEntry in ModManifest[modName])
                {
                    // type being null means we have to figure out the type from the path (StreamingAssets)
                    if (modEntry.Type == null)
                    {
                        var relPath = modEntry.Path.Substring(modEntry.Path.LastIndexOf("StreamingAssets", StringComparison.Ordinal) + 16);
                        var fakeStreamingAssetsPath = Path.GetFullPath(Path.Combine(StreamingAssetsDirectory, relPath));

                        List<string> types;

                        if (TypeCache.ContainsKey(fakeStreamingAssetsPath))
                        {
                            types = TypeCache[fakeStreamingAssetsPath];
                        }
                        else
                        {
                            // get the type from the manifest
                            var matchingEntries = manifest.FindAll(x => Path.GetFullPath(x.FilePath) == fakeStreamingAssetsPath);
                            if (matchingEntries == null || matchingEntries.Count == 0)
                            {
                                // TODO: + 16 is a little bizzare looking, it's the length of the substring + 1 because we want to get rid of it and the \
                                Log($"\t\tCould not find an existing VersionManifest entry for {modEntry.Id}. Is this supposed to be a new entry? Don't put new entries in StreamingAssets!");
                                continue;
                            }

                            types = new List<string>();

                            foreach (var existingEntry in matchingEntries)
                            {
                                types.Add(existingEntry.Type);
                            }

                            TypeCache[fakeStreamingAssetsPath] = types;
                        }

                        if (Path.GetExtension(modEntry.Path).ToLower() == ".json" && modEntry.ShouldMergeJSON)
                        {
                            if (!jsonMerges.ContainsKey(fakeStreamingAssetsPath))
                                jsonMerges[fakeStreamingAssetsPath] = new List<string>();

                            if (jsonMerges[fakeStreamingAssetsPath].Contains(modEntry.Path))
                                continue;

                            // this assumes that .json can only have a single type
                            modEntry.Type = TypeCache[fakeStreamingAssetsPath][0];

                            Log($"\t\tMerge => {modEntry.Id} ({modEntry.Type})");

                            jsonMerges[fakeStreamingAssetsPath].Add(modEntry.Path);
                            continue;
                        }

                        foreach (var type in types)
                        {
                            var subModEntry = new ModDef.ManifestEntry(modEntry, modEntry.Path, modEntry.Id);
                            subModEntry.Type = type;

                            if (AddModEntryToVersionManifest(manifest, subModEntry, breakMyGame))
                                modEntries.Add(modEntry);
                        }

                        continue;
                    }

                    // non-streamingassets json merges
                    if (Path.GetExtension(modEntry.Path).ToLower() == ".json" && modEntry.ShouldMergeJSON)
                    {
                        // have to find the original path for the manifest entry that we're merging onto
                        var matchingEntry = manifest.Find(x => x.Id == modEntry.Id);

                        if (!jsonMerges.ContainsKey(matchingEntry.FilePath))
                            jsonMerges[matchingEntry.FilePath] = new List<string>();

                        if (jsonMerges[matchingEntry.FilePath].Contains(modEntry.Path))
                            continue;

                        // this assumes that .json can only have a single type
                        modEntry.Type = matchingEntry.Type;

                        Log($"\t\tMerge => {modEntry.Id} ({modEntry.Type})");

                        jsonMerges[matchingEntry.FilePath].Add(modEntry.Path);
                        continue;
                    }

                    if (AddModEntryToVersionManifest(manifest, modEntry, breakMyGame))
                        modEntries.Add(modEntry);
                }
            }

            LogWithDate("Doing merges...");
            foreach (var jsonMerge in jsonMerges)
            {
                var cachePath = JsonMergeCache.GetOrCreateCachedEntry(jsonMerge.Key, jsonMerge.Value);
                var cacheEntry = new ModDef.ManifestEntry(cachePath);

                cacheEntry.ShouldMergeJSON = false;
                cacheEntry.Type = TypeCache[jsonMerge.Key][0];
                cacheEntry.Id = InferIDFromFileAndType(cachePath, cacheEntry.Type);

                if (AddModEntryToVersionManifest(manifest, cacheEntry, breakMyGame))
                    modEntries.Add(cacheEntry);
            }

            // write merge cache to disk
            JsonMergeCache.WriteCacheToDisk(Path.Combine(CacheDirectory, MERGE_CACHE_FILE_NAME));

            // write type cache to disk
            File.WriteAllText(Path.Combine(CacheDirectory, TYPE_CACHE_FILE_NAME), JsonConvert.SerializeObject(TypeCache, Formatting.Indented));

            LogWithDate("Done.");
            Log("");
        }
    }
}
