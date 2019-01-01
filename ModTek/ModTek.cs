using BattleTech;
using BattleTech.Data;
using BattleTechModLoader;
using Harmony;
using HBS.Util;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace ModTek
{
    using static Logger;

    public static class ModTek
    {
        public static VersionManifest cachedManifest = null;

        // Lookup for all manifest entries that modtek adds
        public static Dictionary<string, VersionManifestEntry> modtekOverrides = null;

        private static bool hasLoadedMods; //defaults to false

        // file/directory names
        private const string MODS_DIRECTORY_NAME = "Mods";
        private const string MOD_JSON_NAME = "mod.json";
        private const string MODTEK_DIRECTORY_NAME = ".modtek";
        private const string CACHE_DIRECTORY_NAME = "Cache";
        private const string MERGE_CACHE_FILE_NAME = "merge_cache.json";
        private const string TYPE_CACHE_FILE_NAME = "type_cache.json";
        private const string LOG_NAME = "ModTek.log";
        private const string LOAD_ORDER_FILE_NAME = "load_order.json";
        private const string DATABASE_DIRECTORY_NAME = "Database";
        private const string MDD_FILE_NAME = "MetadataDatabase.db";
        private const string DB_CACHE_FILE_NAME = "database_cache.json";
        private const string HARMONY_SUMMARY_FILE_NAME = "harmony_summary.log";

        // files that are read and written to (located in .modtek)
        private static List<string> modLoadOrder;
        private static MergeCache jsonMergeCache;
        private static Dictionary<string, List<string>> typeCache;
        private static Dictionary<string, DateTime> dbCache;

        // things that we added to the VersionManifest, so we don't have to duplicate work when loaded again
        private static List<ModDef.ManifestEntry> modEntries;

        // TODO: remove modManifest
        // pre-loaded mod entries in modName buckets
        private static Dictionary<string, List<ModDef.ManifestEntry>> modManifest = new Dictionary<string, List<ModDef.ManifestEntry>>();

        // Game paths/directories
        public static string GameDirectory { get; private set; }
        public static string ModsDirectory { get; private set; }
        public static string StreamingAssetsDirectory { get; private set; }
        public static string MDDBPath { get; private set; }

        // ModTek paths/directories
        internal static string ModTekDirectory { get; private set; }
        internal static string CacheDirectory { get; private set; }
        internal static string DatabaseDirectory { get; private set; }
        internal static string MergeCachePath { get; private set; }
        internal static string TypeCachePath { get; private set; }
        internal static string ModMDDBPath { get; private set; }
        internal static string DBCachePath { get; private set; }
        internal static string LoadOrderPath { get; private set; }
        internal static string HarmonySummaryPath { get; private set; }

        // non-VersionManifest additions
        internal static Dictionary<string, string> ModAssetBundlePaths { get; } = new Dictionary<string, string>();
        internal static HashSet<string> ModTexture2Ds { get; } = new HashSet<string>();
        internal static Dictionary<string, string> ModVideos { get; } = new Dictionary<string, string>();

        // for timing our loading impact
        private static Stopwatch stopwatch = new Stopwatch();


        // INITIALIZATION (called by BTML)
        [UsedImplicitly]
        public static void Init()
        {
            stopwatch.Start();

            // if the manifest directory is null, there is something seriously wrong
            var manifestDirectory = Path.GetDirectoryName(VersionManifestUtilities.MANIFEST_FILEPATH);
            if (manifestDirectory == null)
                return;

            // setup directories
            ModsDirectory = Path.GetFullPath(
                Path.Combine(manifestDirectory,
                    Path.Combine(Path.Combine(Path.Combine(
                        "..", ".."), ".."), MODS_DIRECTORY_NAME)));

            StreamingAssetsDirectory = Path.GetFullPath(Path.Combine(manifestDirectory, ".."));
            GameDirectory = Path.GetFullPath(Path.Combine(Path.Combine(StreamingAssetsDirectory, ".."), ".."));
            MDDBPath = Path.Combine(Path.Combine(StreamingAssetsDirectory, "MDD"), MDD_FILE_NAME);

            ModTekDirectory = Path.Combine(ModsDirectory, MODTEK_DIRECTORY_NAME);
            CacheDirectory = Path.Combine(ModTekDirectory, CACHE_DIRECTORY_NAME);
            DatabaseDirectory = Path.Combine(ModTekDirectory, DATABASE_DIRECTORY_NAME);

            LogPath = Path.Combine(ModTekDirectory, LOG_NAME);
            HarmonySummaryPath = Path.Combine(ModTekDirectory, HARMONY_SUMMARY_FILE_NAME);
            LoadOrderPath = Path.Combine(ModTekDirectory, LOAD_ORDER_FILE_NAME);
            MergeCachePath = Path.Combine(CacheDirectory, MERGE_CACHE_FILE_NAME);
            TypeCachePath = Path.Combine(CacheDirectory, TYPE_CACHE_FILE_NAME);
            ModMDDBPath = Path.Combine(DatabaseDirectory, MDD_FILE_NAME);
            DBCachePath = Path.Combine(DatabaseDirectory, DB_CACHE_FILE_NAME);

            // creates the directories above it as well
            Directory.CreateDirectory(CacheDirectory);
            Directory.CreateDirectory(DatabaseDirectory);

            // create log file, overwritting if it's already there
            using (var logWriter = File.CreateText(LogPath))
            {
                logWriter.WriteLine($"ModTek v{Assembly.GetExecutingAssembly().GetName().Version} -- {DateTime.Now}");
            }

            // create all of the caches
            dbCache = LoadOrCreateDBCache(DBCachePath);
            jsonMergeCache = LoadOrCreateMergeCache(MergeCachePath);
            typeCache = LoadOrCreateTypeCache(TypeCachePath);

            // First step in setting up the progress panel
            if (ProgressPanel.Initialize(ModsDirectory, $"ModTek v{Assembly.GetExecutingAssembly().GetName().Version}"))
            {
                // init harmony and patch the stuff that comes with ModTek (contained in Patches.cs)
                var harmony = HarmonyInstance.Create("io.github.mpstark.ModTek");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                LoadMods();

                BuildCachedManifest();
            }
            else
            {
                Log("Failed to load progress bar.  Skipping mod loading completely.");
            }

            stopwatch.Stop();
        }


        // LOAD ORDER
        private static void PropagateConflictsForward(Dictionary<string, ModDef> modDefs)
        {
            // conflicts are a unidirectional edge, so make them one in ModDefs
            foreach (var modDefKvp in modDefs)
            {
                var modDef = modDefKvp.Value;
                if (modDef.ConflictsWith.Count == 0)
                    continue;

                foreach (var conflict in modDef.ConflictsWith)
                {
                    if (modDefs.ContainsKey(conflict))
                        modDefs[conflict].ConflictsWith.Add(modDef.Name);
                }
            }
        }

        private static List<string> LoadLoadOrder(string path)
        {
            List<string> order;

            if (File.Exists(path))
                try
                {
                    order = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(path));
                    Log("Loaded cached load order.");
                    return order;
                }
                catch (Exception e)
                {
                    Log("Loading cached load order failed, rebuilding it.");
                    Log($"\t{e.Message}");
                }

            // create a new one if it doesn't exist or couldn't be added
            Log("Building new load order!");
            order = new List<string>();
            return order;
        }

        private static bool AreDependanciesResolved(ModDef modDef, HashSet<string> loaded)
        {
            return !(modDef.DependsOn.Count != 0 && modDef.DependsOn.Intersect(loaded).Count() != modDef.DependsOn.Count
                || modDef.ConflictsWith.Count != 0 && modDef.ConflictsWith.Intersect(loaded).Any());
        }

        private static List<string> GetLoadOrder(Dictionary<string, ModDef> modDefs, out List<string> unloaded)
        {
            var modDefsCopy = new Dictionary<string, ModDef>(modDefs);
            var cachedOrder = LoadLoadOrder(LoadOrderPath);
            var loadOrder = new List<string>();
            var loaded = new HashSet<string>();

            // load the order specified in the file
            foreach (var modName in cachedOrder)
            {
                if (!modDefs.ContainsKey(modName) || !AreDependanciesResolved(modDefs[modName], loaded)) continue;

                modDefsCopy.Remove(modName);
                loadOrder.Add(modName);
                loaded.Add(modName);
            }

            // everything that is left in the copy hasn't been loaded before
            unloaded = modDefsCopy.Keys.OrderByDescending(x => x).ToList();

            // there is nothing left to load
            if (unloaded.Count == 0)
                return loadOrder;

            // this is the remainder that haven't been loaded before
            int removedThisPass;
            do
            {
                removedThisPass = 0;

                for (var i = unloaded.Count - 1; i >= 0; i--)
                {
                    var modDef = modDefs[unloaded[i]];

                    if (!AreDependanciesResolved(modDef, loaded)) continue;

                    unloaded.RemoveAt(i);
                    loadOrder.Add(modDef.Name);
                    loaded.Add(modDef.Name);
                    removedThisPass++;
                }
            } while (removedThisPass > 0 && unloaded.Count > 0);

            return loadOrder;
        }

        // LOADING MODS
        private static void LoadMod(ModDef modDef)
        {
            var potentialAdditions = new List<ModDef.ManifestEntry>();

            Log($"Loading {modDef.Name} {modDef.Version}");

            // load out of the manifest
            if (modDef.LoadImplicitManifest && modDef.Manifest.All(x => Path.GetFullPath(Path.Combine(modDef.Directory, x.Path)) != Path.GetFullPath(Path.Combine(modDef.Directory, "StreamingAssets"))))
                modDef.Manifest.Add(new ModDef.ManifestEntry("StreamingAssets", true));

            // note: if a JSON has errors, this mod will not load, since InferIDFromFile will throw from parsing the JSON
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
                    if (!FileIsOnDenyList(entry.Path)) potentialAdditions.Add(entry);
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
                    var files = Directory.GetFiles(entryPath, "*", SearchOption.AllDirectories).Where(filePath => !FileIsOnDenyList(filePath));
                    foreach (var filePath in files)
                    {
                        var childModDef = new ModDef.ManifestEntry(entry, filePath, InferIDFromFile(filePath));
                        potentialAdditions.Add(childModDef);
                    }
                }
                else if (File.Exists(entryPath) && !FileIsOnDenyList(entryPath))
                {
                    // path is a file, add the single entry
                    entry.Id = entry.Id ?? InferIDFromFile(entryPath);
                    entry.Path = entryPath;
                    potentialAdditions.Add(entry);
                }
                else if (entry.Path != "StreamingAssets")
                {
                    // path is not streamingassets and it's missing
                    Log($"\tMissing Entry: Manifest specifies file/directory of {entry.Type} at path {entry.Path}, but it's not there. Continuing to load.");
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

            if (potentialAdditions.Count <= 0)
                return;

            // actually add the additions, since we successfully got through loading the other stuff
            foreach (var addition in potentialAdditions)
                Log($"\tNew Entry: {addition.Path.Replace(ModsDirectory, "")}");

            modManifest[modDef.Name] = potentialAdditions;
        }

        internal static void LoadMods()
        {
            ProgressPanel.SubmitWork(ModTek.LoadMoadsLoop);
        }

        internal static IEnumerator<ProgressReport> LoadMoadsLoop()
        {
            // Only want to run this function once -- it could get submitted a few times
            if (hasLoadedMods)
            {
                yield break;
            }

            stopwatch.Start();

            string sliderText = "Loading Mods";

            Log("");
            LogWithDate("Pre-loading mods...");
            yield return new ProgressReport(0, sliderText, "Pre-loading mods...");

            // find all sub-directories that have a mod.json file
            var modDirectories = Directory.GetDirectories(ModsDirectory)
                .Where(x => File.Exists(Path.Combine(x, MOD_JSON_NAME))).ToArray();

            if (modDirectories.Length == 0)
            {
                hasLoadedMods = true;
                Log("No ModTek-compatable mods found.");
                yield break;
            }

            // create ModDef objects for each mod.json file
            var modDefs = new Dictionary<string, ModDef>();
            foreach (var modDirectory in modDirectories)
            {
                ModDef modDef;
                var modDefPath = Path.Combine(modDirectory, MOD_JSON_NAME);

                try
                {
                    modDef = ModDef.CreateFromPath(modDefPath);
                }
                catch (Exception e)
                {
                    Log($"Caught exception while parsing {MOD_JSON_NAME} at path {modDefPath}");
                    Log($"\t{e.Message}");
                    continue;
                }

                if (!modDef.Enabled)
                {
                    Log($"Will not load {modDef.Name} because it's disabled.");
                    continue;
                }

                if (modDefs.ContainsKey(modDef.Name))
                {
                    Log($"Already loaded a mod named {modDef.Name}. Skipping load from {modDef.Directory}.");
                    continue;
                }

                modDefs.Add(modDef.Name, modDef);
            }

            PropagateConflictsForward(modDefs);
            modLoadOrder = GetLoadOrder(modDefs, out var willNotLoad);

            int modLoaded = 0;
            // lists guarentee order
            foreach (var modName in modLoadOrder)
            {
                var modDef = modDefs[modName];
                yield return new ProgressReport(
                    (float)modLoaded++/(float)modLoadOrder.Count,
                    sliderText,
                    string.Format("Loading Mod: {0} {1}", modDef.Name, modDef.Version)
                );
                try
                {
                    LoadMod(modDef);
                }
                catch (Exception e)
                {
                    Log($"Tried to load mod: {modDef.Name}, but something went wrong. Make sure all of your JSON is correct!");
                    Log($"\t{e.Message}");
                }
            }

            foreach (var modDef in willNotLoad)
            {
                Log($"Will not load {modDef}. It's lacking a dependancy or a conflict loaded before it.");
            }

            stopwatch.Stop();
            Log("");
            LogWithDate($"Done pre-load mods. Elapsed running time: {stopwatch.Elapsed.TotalSeconds} seconds\n");
            Log("----------\n");

            // write out harmony summary
            PrintHarmonySummary(HarmonySummaryPath);

            // write out load order
            File.WriteAllText(LoadOrderPath, JsonConvert.SerializeObject(modLoadOrder, Formatting.Indented));

            hasLoadedMods = true;

            yield break;
        }

        private static string InferIDFromFile(string path)
        {
            // if not json, return the file name without the extension, as this is what HBS uses
            var ext = Path.GetExtension(path);
            if (ext == null || ext.ToLower() != ".json" || !File.Exists(path))
                return Path.GetFileNameWithoutExtension(path);

            // read the json and get ID out of it if able to
            return InferIDFromJObject(ParseGameJSON(File.ReadAllText(path))) ?? Path.GetFileNameWithoutExtension(path);
        }

        private static void PrintHarmonySummary(string path)
        {
            var harmony = HarmonyInstance.Create("io.github.mpstark.ModTek");

            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            if (patchedMethods.Length == 0)
                return;

            using (var writer = File.CreateText(path))
            {
                writer.WriteLine($"Harmony Patched Methods (after ModTek startup) -- {DateTime.Now}\n");

                foreach (var method in patchedMethods)
                {
                    var info = harmony.GetPatchInfo(method);

                    if (info == null || method.ReflectedType == null)
                        continue;

                    writer.WriteLine($"{method.ReflectedType.FullName}.{method.Name}:");

                    // prefixes
                    if (info.Prefixes.Count != 0)
                        writer.WriteLine("\tPrefixes:");
                    foreach (var patch in info.Prefixes)
                        writer.WriteLine($"\t\t{patch.owner}");

                    // transpilers
                    if (info.Transpilers.Count != 0)
                        writer.WriteLine("\tTranspilers:");
                    foreach (var patch in info.Transpilers)
                        writer.WriteLine($"\t\t{patch.owner}");

                    // postfixes
                    if (info.Postfixes.Count != 0)
                        writer.WriteLine("\tPostfixes:");
                    foreach (var patch in info.Postfixes)
                        writer.WriteLine($"\t\t{patch.owner}");

                    writer.WriteLine("");
                }
            }
        }

        // this is a very rudimentary version of an .ignore list for filetypes
        // I have added it to remove my most common problems. PRs welcome.
        private static readonly string[] ignoreList = { ".DS_STORE", "~", ".nomedia" };
        private static bool FileIsOnDenyList(string filePath)
        {
            return ignoreList.Any(x => filePath.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
        }

        // JSON HANDLING
        /// <summary>
        ///     Create JObject from string, removing comments and adding commas first.
        /// </summary>
        /// <param name="jsonText">JSON contained in a string</param>
        /// <returns>JObject parsed from jsonText, null if invalid</returns>
        internal static JObject ParseGameJSON(string jsonText)
        {
            // because StripHBSCommentsFromJSON is private, use Harmony to call the method
            var commentsStripped = Traverse.Create(typeof(JSONSerializationUtility)).Method("StripHBSCommentsFromJSON", jsonText).GetValue() as string;

            if (commentsStripped == null)
                throw new Exception("StripHBSCommentsFromJSON returned null.");

            // add missing commas, this only fixes if there is a newline
            var rgx = new Regex(@"(\]|\}|""|[A-Za-z0-9])\s*\n\s*(\[|\{|"")", RegexOptions.Singleline);
            var commasAdded = rgx.Replace(commentsStripped, "$1,\n$2");

            return JObject.Parse(commasAdded);
        }

        internal static JObject ParseGameJSONFile(string path)
        {
            return ParseGameJSON(File.ReadAllText(path));
        }

        private static string InferIDFromJObject(JObject jObj)
        {
            if (jObj == null)
                return null;

            // go through the different kinds of id storage in JSONS
            string[] jPaths = { "Description.Id", "id", "Id", "ID", "identifier", "Identifier" };
            foreach (var jPath in jPaths)
            {
                var id = (string)jObj.SelectToken(jPath);
                if (id != null)
                    return id;
            }

            return null;
        }


        // CACHES
        internal static MergeCache LoadOrCreateMergeCache(string path)
        {
            MergeCache mergeCache;

            if (File.Exists(path))
            {
                try
                {
                    mergeCache = JsonConvert.DeserializeObject<MergeCache>(File.ReadAllText(path));
                    Log("Loaded merge cache.");
                    return mergeCache;
                }
                catch (Exception e)
                {
                    Log("Loading merge cache failed -- will rebuild it.");
                    Log($"\t{e.Message}");
                }
            }

            // create a new one if it doesn't exist or couldn't be added'
            Log("Building new Merge Cache.");
            mergeCache = new MergeCache();
            return mergeCache;
        }

        internal static Dictionary<string, List<string>> LoadOrCreateTypeCache(string path)
        {
            Dictionary<string, List<string>> cache;

            if (File.Exists(path))
            {
                try
                {
                    cache = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(path));
                    Log("Loaded type cache.");
                    return cache;
                }
                catch (Exception e)
                {
                    Log("Loading type cache failed -- will rebuild it.");
                    Log($"\t{e.Message}");
                }
            }

            // create a new one if it doesn't exist or couldn't be added
            Log("Building new Type Cache.");
            cache = new Dictionary<string, List<string>>();
            return cache;
        }

        internal static List<string> GetTypesFromCacheOrManifest(VersionManifest manifest, string path)
        {
            if (typeCache.ContainsKey(path))
            {
                return typeCache[path];
            }

            // get the type from the manifest
            var matchingEntries = manifest.FindAll(x => Path.GetFullPath(x.FilePath) == path);

            if (matchingEntries == null || matchingEntries.Count == 0)
                return null;

            var types = new List<string>();

            foreach (var existingEntry in matchingEntries)
                types.Add(existingEntry.Type);

            typeCache[path] = types;
            return typeCache[path];
        }

        internal static Dictionary<string, DateTime> LoadOrCreateDBCache(string path)
        {
            Dictionary<string, DateTime> cache;

            if (File.Exists(path) && File.Exists(ModMDDBPath))
            {
                try
                {
                    cache = JsonConvert.DeserializeObject<Dictionary<string, DateTime>>(File.ReadAllText(path));
                    Log("Loaded db cache.");
                    return cache;
                }
                catch (Exception e)
                {
                    Log("Loading db cache failed -- will rebuild it.");
                    Log($"\t{e.Message}");
                }
            }

            // delete mod db if it exists the cache does not
            if (File.Exists(ModMDDBPath))
                File.Delete(ModMDDBPath);

            File.Copy(Path.Combine(Path.Combine(StreamingAssetsDirectory, "MDD"), MDD_FILE_NAME), ModMDDBPath);

            // create a new one if it doesn't exist or couldn't be added
            Log("Copying over DB and building new DB Cache.");
            cache = new Dictionary<string, DateTime>();
            return cache;
        }

        internal static void WriteJsonFile(string path, object obj)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(obj, Formatting.Indented));
        }


        // ADDING TO VERSION MANIFEST
        private static bool AddModEntry(VersionManifest manifest, ModDef.ManifestEntry modEntry)
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

            // add special handling for particular types
            switch (modEntry.Type)
            {
                case "AssetBundle":
                    ModAssetBundlePaths[modEntry.Id] = modEntry.Path;
                    break;
                case "Texture2D":
                    ModTexture2Ds.Add(modEntry.Id);
                    break;
            }

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

        private static bool AddModEntryToDB(MetadataDatabase db, string path, string typeStr)
        {
            if (Path.GetExtension(path)?.ToLower() != ".json")
                return false;

            var type = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), typeStr);

            switch (type) // switch is to avoid poisoning the output_log.txt with known types that don't use MDD
            {
                case BattleTechResourceType.TurretDef:
                case BattleTechResourceType.UpgradeDef:
                case BattleTechResourceType.VehicleDef:
                case BattleTechResourceType.ContractOverride:
                case BattleTechResourceType.SimGameEventDef:
                case BattleTechResourceType.LanceDef:
                case BattleTechResourceType.MechDef:
                case BattleTechResourceType.PilotDef:
                case BattleTechResourceType.WeaponDef:
                    if (!dbCache.ContainsKey(path) || dbCache[path] != File.GetLastWriteTimeUtc(path))
                    {
                        try
                        {
                            VersionManifestHotReload.InstantiateResourceAndUpdateMDDB(type, path, db);
                            dbCache[path] = File.GetLastWriteTimeUtc(path);
                            return true;
                        }
                        catch (Exception e)
                        {
                            Log($"\tAdd to DB failed for {Path.GetFileName(path)}, exception caught:");
                            Log($"\t\t{e.Message}");
                            return false;
                        }
                    }
                    break;
            }

            return false;
        }

        internal static void BuildCachedManifest()
        {
            // First load the default battletech manifest, then it'll get appended to
            VersionManifest vanillaManifest = VersionManifestUtilities.LoadDefaultManifest();

            // Wrapper to be able to submit a parameterless work function
            IEnumerator<ProgressReport> NestedFunc()
            {
                IEnumerator<ProgressReport> reports = BuildCachedManifestLoop(vanillaManifest);
                while (reports.MoveNext())
                {
                    yield return reports.Current;
                }
            }

            ProgressPanel.SubmitWork(NestedFunc);
        }

        internal static string ResolvePath(string path)
        {
            path = path.Replace("{{Mods}}", ModsDirectory);

            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(StreamingAssetsDirectory, path);
            }

            var normalizedPath = Path.GetFullPath(path);
            return normalizedPath;
        }

        internal static IEnumerator<ProgressReport> BuildCachedManifestLoop(VersionManifest manifest) { 

            stopwatch.Start();

            // there are no mods loaded, just return
            if (modLoadOrder == null || modLoadOrder.Count == 0)
                yield break;

            string loadingModText = "Loading Mod Manifests";
            yield return new ProgressReport(0.0f, loadingModText, "Setting up mod manifests...");

            LogWithDate("Setting up mod manifests...");

            var jsonMerges = new Dictionary<string, List<string>>();
            modEntries = new List<ModDef.ManifestEntry>();
            int modCount = 0;

            var manifestMods = modLoadOrder.Where(name => modManifest.ContainsKey(name)).ToList();
            foreach (var modName in manifestMods)
            {
                Log($"\t{modName}:");
                yield return new ProgressReport((float)modCount++/(float)manifestMods.Count, loadingModText, string.Format("Loading manifest for {0}", modName));
                foreach (var modEntry in modManifest[modName])
                {
                    // type being null means we have to figure out the type from the path (StreamingAssets)
                    if (modEntry.Type == null)
                    {
                        // TODO: + 16 is a little bizzare looking, it's the length of the substring + 1 because we want to get rid of it and the \
                        var relPath = modEntry.Path.Substring(modEntry.Path.LastIndexOf("StreamingAssets", StringComparison.Ordinal) + 16);
                        var fakeStreamingAssetsPath = Path.GetFullPath(Path.Combine(StreamingAssetsDirectory, relPath));

                        var types = GetTypesFromCacheOrManifest(manifest, fakeStreamingAssetsPath);

                        if (types == null)
                        {
                            Log($"\t\tCould not find an existing VersionManifest entry for {modEntry.Id}. Is this supposed to be a new entry? Don't put new entries in StreamingAssets!");
                            continue;
                        }

                        if (Path.GetExtension(modEntry.Path).ToLower() == ".json" && modEntry.ShouldMergeJSON)
                        {
                            if (!jsonMerges.ContainsKey(fakeStreamingAssetsPath))
                                jsonMerges[fakeStreamingAssetsPath] = new List<string>();

                            if (jsonMerges[fakeStreamingAssetsPath].Contains(modEntry.Path))
                                continue;

                            // this assumes that .json can only have a single type
                            // typeCache will always contain this path
                            modEntry.Type = typeCache[fakeStreamingAssetsPath][0];

                            Log($"\t\tMerge => {modEntry.Id} ({modEntry.Type})");

                            jsonMerges[fakeStreamingAssetsPath].Add(modEntry.Path);
                            continue;
                        }

                        foreach (var type in types)
                        {
                            var subModEntry = new ModDef.ManifestEntry(modEntry, modEntry.Path, modEntry.Id);
                            subModEntry.Type = type;

                            if (AddModEntry(manifest, subModEntry))
                                modEntries.Add(subModEntry);
                        }

                        continue;
                    }

                    // get "fake" entries that don't actually go into the game's VersionManifest
                    // add videos to be loaded from an external path
                    switch (modEntry.Type)
                    {
                        case "Video":
                            var fileName = Path.GetFileName(modEntry.Path);
                            if (fileName != null && File.Exists(modEntry.Path))
                            {
                                Log($"\t\tVideo => {fileName}");
                                ModVideos.Add(fileName, modEntry.Path);
                            }
                            continue;
                        case "AdvancedJSONMerge":
                            var targetFileRelative = AdvancedJSONMerger.GetTargetFile(modEntry.Path);
                            var targetFile = ResolvePath(targetFileRelative);

                            // need to add the types of the file to the typeCache, so that they can be used later
                            // this actually returns the type, but we don't actually care about that right now
                            GetTypesFromCacheOrManifest(manifest, targetFile);

                            if (!jsonMerges.ContainsKey(targetFile))
                                jsonMerges[targetFile] = new List<string>();

                            if (jsonMerges[targetFile].Contains(modEntry.Path))
                                continue;

                            Log($"\t\tAdvancedJSONMerge => {modEntry.Id} ({modEntry.Type})");
                            jsonMerges[targetFile].Add(modEntry.Path);
                            continue;
                    }

                    // non-streamingassets json merges
                    if (Path.GetExtension(modEntry.Path)?.ToLower() == ".json" && modEntry.ShouldMergeJSON)
                    {
                        // have to find the original path for the manifest entry that we're merging onto
                        var matchingEntry = manifest.Find(x => x.Id == modEntry.Id);

                        if (matchingEntry == null)
                        {
                            Log($"\t\tCould not find an existing VersionManifest entry for {modEntry.Id}!");
                            continue;
                        }

                        if (!jsonMerges.ContainsKey(matchingEntry.FilePath))
                            jsonMerges[matchingEntry.FilePath] = new List<string>();

                        if (jsonMerges[matchingEntry.FilePath].Contains(modEntry.Path))
                            continue;

                        // this assumes that .json can only have a single type
                        modEntry.Type = matchingEntry.Type;

                        if (!typeCache.ContainsKey(matchingEntry.FilePath))
                        {
                            typeCache[matchingEntry.FilePath] = new List<string>();
                            typeCache[matchingEntry.FilePath].Add(modEntry.Type);
                        }

                        Log($"\t\tMerge => {modEntry.Id} ({modEntry.Type})");

                        jsonMerges[matchingEntry.FilePath].Add(modEntry.Path);
                        continue;
                    }

                    if (AddModEntry(manifest, modEntry))
                        modEntries.Add(modEntry);
                }
            }

            yield return new ProgressReport(100.0f, "JSON", "Writing JSON file to disk");

            // write type cache to disk
            WriteJsonFile(TypeCachePath, typeCache);

            // perform merges into cache
            LogWithDate("Doing merges...");
            yield return new ProgressReport(0.0f, "Merges", "Doing Merges...");
            int mergeCount = 0;
            foreach (var jsonMerge in jsonMerges)
            {
                yield return new ProgressReport((float)mergeCount++/jsonMerges.Count, "Merges", string.Format("Merging {0}", jsonMerge.Key));
                var cachePath = jsonMergeCache.GetOrCreateCachedEntry(jsonMerge.Key, jsonMerge.Value);

                // something went wrong (the parent json prob had errors)
                if (cachePath == null)
                    continue;

                var cacheEntry = new ModDef.ManifestEntry(cachePath);

                cacheEntry.ShouldMergeJSON = false;
                cacheEntry.Type = typeCache[jsonMerge.Key][0]; // this assumes only one type for each json file
                cacheEntry.Id = InferIDFromFile(cachePath);

                if (AddModEntry(manifest, cacheEntry))
                    modEntries.Add(cacheEntry);
            }

            yield return new ProgressReport(100.0f, "Merge Cache", "Writing Merge Cache to disk");

            // write merge cache to disk
            jsonMergeCache.WriteCacheToDisk(Path.Combine(CacheDirectory, MERGE_CACHE_FILE_NAME));

            LogWithDate("Adding to DB...");

            // check if files removed from DB cache
            var rebuildDB = false;
            var replacementEntries = new List<VersionManifestEntry>();
            var removeEntries = new List<string>();

            string dbText = "Syncing Database";
            yield return new ProgressReport(0.0f, dbText, "");
            foreach (var kvp in dbCache)
            {
                var path = kvp.Key;

                if (File.Exists(path))
                    continue;

                Log($"\tNeed to remove DB entry from file in path: {path}");

                // file is missing, check if another entry exists with same filename in manifest
                var fileName = Path.GetFileName(path);
                var existingEntry = manifest.Find(x => Path.GetFileName(x.FilePath) == fileName);

                if (existingEntry == null)
                {
                    Log("\t\tHave to rebuild DB, no existing entry in VersionManifest matches removed entry");
                    rebuildDB = true;
                    break;
                }

                replacementEntries.Add(existingEntry);
                removeEntries.Add(path);
            }

            // add removed entries replacements to db
            dbText = "Cleaning Database";
            yield return new ProgressReport(100.0f, dbText, "");
            if (!rebuildDB)
            {
                // remove old entries
                foreach (var removeEntry in removeEntries)
                    dbCache.Remove(removeEntry);

                using (var metadataDatabase = new MetadataDatabase())
                {
                    foreach (var replacementEntry in replacementEntries)
                    {
                        if (AddModEntryToDB(metadataDatabase, Path.GetFullPath(replacementEntry.FilePath), replacementEntry.Type))
                            Log($"\t\tReplaced DB entry with an existing entry in path: {Path.GetFullPath(replacementEntry.FilePath)}");
                    }
                }
            }

            // if an entry has been removed and we cannot find a replacement, have to rebuild the mod db
            if (rebuildDB)
            {
                if (File.Exists(ModMDDBPath))
                    File.Delete(ModMDDBPath);

                File.Copy(MDDBPath, ModMDDBPath);
                dbCache = new Dictionary<string, DateTime>();
            }

            // add needed files to db
            dbText = "Populating Database";
            int addCount = 0;
            yield return new ProgressReport(0.0f, dbText, "");
            using (var metadataDatabase = new MetadataDatabase())
            {
                foreach (var modEntry in modEntries)
                {
                    if (modEntry.AddToDB && AddModEntryToDB(metadataDatabase, modEntry.Path, modEntry.Type))
                    {
                        yield return new ProgressReport((float)addCount / (float)modEntries.Count, dbText, string.Format("Added {0}", modEntry.Path));
                        Log($"\tAdded/Updated {modEntry.Id} ({modEntry.Type})");
                    }
                    addCount++;
                }
            }

            // write db/type cache to disk
            WriteJsonFile(DBCachePath, dbCache);

            stopwatch.Stop();
            Log("");
            LogWithDate($"Done. Elapsed running time: {stopwatch.Elapsed.TotalSeconds} seconds\n");

            // Cache the completed manifest
            ModTek.cachedManifest = manifest;

            try
            {
                if (manifest != null && ModTek.modEntries != null)
                {
                    ModTek.modtekOverrides = manifest.Entries.Where(e => ModTek.modEntries.Any(m => e.Id == m.Id))
                        // ToDictionary expects distinct keys, so take the last entry of each Id
                        .GroupBy(ks => ks.Id)
                        .Select(v => v.Last())
                        .ToDictionary(ks => ks.Id);
                }
                Logger.Log("Built {0} modtek overrides", ModTek.modtekOverrides.Count());
            }
            catch (Exception e)
            {
                Logger.Log("Failed to build overrides {0}", e);
            }

            yield break;
        }
    }
}
