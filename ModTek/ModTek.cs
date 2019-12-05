using BattleTech;
using BattleTech.Data;
using Harmony;
using HBS.Util;
using ModTek.Caches;
using ModTek.RuntimeLog;
using ModTek.UI;
using ModTek.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static ModTek.Util.Logger;

namespace ModTek
{
    public class Settings
    {
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;
    }
    public static class ModTek
    {
        private static readonly string[] IGNORE_LIST = { ".DS_STORE", "~", ".nomedia" };
        private static readonly string[] MODTEK_TYPES = { "Video", "AdvancedJSONMerge", "GameTip", "SoundBank", "DebugSettings" };
        private static readonly string[] VANILLA_TYPES = Enum.GetNames(typeof(BattleTechResourceType));

        public static Settings settings { get; private set; } = new Settings();

        public static bool HasLoaded { get; private set; }

        // game paths/directories
        public static string GameDirectory { get; private set; }
        public static string ModsDirectory { get; private set; }
        public static string StreamingAssetsDirectory { get; private set; }
        public static ModDefEx SettingsDef { get; private set; }

        public static bool Enabled { get { return SettingsDef.Enabled; } }

        // file/directory names
        private const string MODS_DIRECTORY_NAME = "Mods";
        public const string MOD_JSON_NAME = "mod.json";
        public const string MOD_STATE_JSON_NAME = "modstate.json";
        private const string MODTEK_DIRECTORY_NAME = "ModTek";
        private const string TEMP_MODTEK_DIRECTORY_NAME = ".modtek";
        private const string CACHE_DIRECTORY_NAME = "Cache";
        private const string MERGE_CACHE_FILE_NAME = "merge_cache.json";
        private const string TYPE_CACHE_FILE_NAME = "type_cache.json";
        private const string LOG_NAME = "ModTek.log";
        private const string LOAD_ORDER_FILE_NAME = "load_order.json";
        private const string DATABASE_DIRECTORY_NAME = "Database";
        private const string MDD_FILE_NAME = "MetadataDatabase.db";
        private const string DB_CACHE_FILE_NAME = "database_cache.json";
        private const string HARMONY_SUMMARY_FILE_NAME = "harmony_summary.log";
        private const string CONFIG_FILE_NAME = "config.json";
        public const string MODTEK_DEF_NAME = "ModTek";

        // ModTek paths/directories
        internal static string ModTekDirectory { get; private set; }
        internal static string TempModTekDirectory { get; private set; }
        internal static string CacheDirectory { get; private set; }
        internal static string DatabaseDirectory { get; private set; }
        internal static string MergeCachePath { get; private set; }
        internal static string TypeCachePath { get; private set; }
        internal static string MDDBPath { get; private set; }
        internal static string ModMDDBPath { get; private set; }
        internal static string DBCachePath { get; private set; }
        internal static string LoadOrderPath { get; private set; }
        internal static string HarmonySummaryPath { get; private set; }
        internal static string ConfigPath { get; private set; }
        internal static string ModTekSettingsPath { get; private set; }

        // special StreamingAssets relative directories
        internal static string DebugSettingsPath { get; } = Path.Combine(Path.Combine("data", "debug"), "settings.json");

        // internal temp structures
        private static Stopwatch stopwatch = new Stopwatch();
        private static Dictionary<string, JObject> cachedJObjects = new Dictionary<string, JObject>();
        private static Dictionary<string, Dictionary<string, List<string>>> merges = new Dictionary<string, Dictionary<string, List<string>>>();

        // internal structures
        internal static Configuration Config;
        internal static List<string> ModLoadOrder;
        internal static Dictionary<string, ModDefEx> ModDefs = new Dictionary<string, ModDefEx>();
        public static Dictionary<string, ModDefEx> allModDefs = new Dictionary<string, ModDefEx>();
        internal static HashSet<string> FailedToLoadMods { get; } = new HashSet<string>();
        internal static Dictionary<string, Assembly> TryResolveAssemblies = new Dictionary<string, Assembly>();

        // the end result of loading mods, these are used to push into game data through patches
        internal static VersionManifest CachedVersionManifest;
        internal static List<ModEntry> AddBTRLEntries = new List<ModEntry>();
        internal static List<VersionManifestEntry> RemoveBTRLEntries = new List<VersionManifestEntry>();
        internal static Dictionary<string, Dictionary<string, VersionManifestEntry>> CustomResources = new Dictionary<string, Dictionary<string, VersionManifestEntry>>();
        internal static Dictionary<string, string> ModAssetBundlePaths { get; } = new Dictionary<string, string>();

        // INITIALIZATION (called by injected code)
        public static void Init()
        {
            if (HasLoaded)
                return;

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
            TempModTekDirectory = Path.Combine(ModsDirectory, TEMP_MODTEK_DIRECTORY_NAME);
            CacheDirectory = Path.Combine(TempModTekDirectory, CACHE_DIRECTORY_NAME);
            DatabaseDirectory = Path.Combine(TempModTekDirectory, DATABASE_DIRECTORY_NAME);

            LogPath = Path.Combine(TempModTekDirectory, LOG_NAME);
            HarmonySummaryPath = Path.Combine(TempModTekDirectory, HARMONY_SUMMARY_FILE_NAME);
            LoadOrderPath = Path.Combine(TempModTekDirectory, LOAD_ORDER_FILE_NAME);
            MergeCachePath = Path.Combine(CacheDirectory, MERGE_CACHE_FILE_NAME);
            TypeCachePath = Path.Combine(CacheDirectory, TYPE_CACHE_FILE_NAME);
            ModMDDBPath = Path.Combine(DatabaseDirectory, MDD_FILE_NAME);
            DBCachePath = Path.Combine(DatabaseDirectory, DB_CACHE_FILE_NAME);
            ConfigPath = Path.Combine(TempModTekDirectory, CONFIG_FILE_NAME);
            ModTekSettingsPath = Path.Combine(ModTekDirectory, MOD_JSON_NAME);

            // creates the directories above it as well
            Directory.CreateDirectory(CacheDirectory);
            Directory.CreateDirectory(DatabaseDirectory);

            var versionString = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            RLog.InitLog(TempModTekDirectory, true);
            RLog.M.TWL(0,"Init ModTek vesrion " + Assembly.GetExecutingAssembly().GetName().Version);

            // create log file, overwriting if it's already there
            using (var logWriter = File.CreateText(LogPath))
            {
                logWriter.WriteLine($"ModTek v{versionString} -- {DateTime.Now}");
            }
            if (File.Exists(ModTekSettingsPath))
            {
                try
                {
                    SettingsDef = ModDefEx.CreateFromPath(ModTekSettingsPath);
                }
                catch (Exception e)
                {
                    LogException($"Error: Caught exception while parsing {MOD_JSON_NAME} at path {ModTekSettingsPath}", e);
                    Finish();
                    return;
                }
                SettingsDef.Version = versionString;
            }
            else
            {
                Log("File not exists "+ ModTekSettingsPath+" fallback to defaults");
                SettingsDef = new ModDefEx();
                SettingsDef.Enabled = true;
                SettingsDef.PendingEnable = true;
                SettingsDef.Name = MODTEK_DEF_NAME;
                SettingsDef.Version = versionString;
                SettingsDef.Description = "Mod system for HBS's PC game BattleTech.";
                SettingsDef.Author = "Mpstark, CptMoore, Tyler-IN, alexbartlow, janxious, m22spencer, KMiSSioN, ffaristocrat, Morphyum";
                SettingsDef.Website = "https://github.com/BattletechModders/ModTek";
                File.WriteAllText(ModTekSettingsPath,JsonConvert.SerializeObject(SettingsDef,Formatting.Indented));
                SettingsDef.Directory = ModTekDirectory;
                SettingsDef.SaveState();
            }


            // load progress bar
            if (Enabled)
            {
                if (!ProgressPanel.Initialize(ModTekDirectory, $"ModTek v{versionString}"))
                {
                    Log("Error: Failed to load progress bar.  Skipping mod loading completely.");
                    Finish();
                }
            }

            // read config
            Config = Configuration.FromFile(ConfigPath);

            // setup assembly resolver
            TryResolveAssemblies.Add("0Harmony", Assembly.GetAssembly(typeof(HarmonyInstance)));
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var resolvingName = new AssemblyName(args.Name);
                return !TryResolveAssemblies.TryGetValue(resolvingName.Name, out var assembly) ? null : assembly;
            };

            try
            {
                HarmonyInstance.Create("io.github.mpstark.ModTek").PatchAll(Assembly.GetExecutingAssembly());
            }
            catch (Exception e)
            {
                LogException("Error: PATCHING FAILED!", e);
                CloseLogStream();
                return;
            }
            if (Enabled == false)
            {
                Log("ModTek not enabled");
                CloseLogStream();
                return;
            }
            LoadMods();
        }

        internal static void Finish()
        {
            HasLoaded = true;

            stopwatch.Stop();
            Log("");
            LogWithDate($"Done. Elapsed running time: {stopwatch.Elapsed.TotalSeconds} seconds\n");

            CloseLogStream();

            // clear temp objects
            cachedJObjects = null;
            merges = null;
            stopwatch = null;
        }


        // PATHS
        internal static string ResolvePath(string path, string rootPathToUse)
        {
            if (!Path.IsPathRooted(path))
                path = Path.Combine(rootPathToUse, path);

            return Path.GetFullPath(path);
        }

        internal static string GetRelativePath(string path, string rootPath)
        {
            if (!Path.IsPathRooted(path))
                return path;

            rootPath = Path.GetFullPath(rootPath);
            if (rootPath.Last() != Path.DirectorySeparatorChar)
                rootPath += Path.DirectorySeparatorChar;

            var pathUri = new Uri(Path.GetFullPath(path), UriKind.Absolute);
            var rootUri = new Uri(rootPath, UriKind.Absolute);

            if (pathUri.Scheme != rootUri.Scheme)
                return path;

            var relativeUri = rootUri.MakeRelativeUri(pathUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (pathUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return relativePath;
        }


        // UTIL
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

        private static bool FileIsOnDenyList(string filePath)
        {
            return IGNORE_LIST.Any(x => filePath.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
        }

        internal static JObject ParseGameJSONFile(string path)
        {
            if (cachedJObjects.ContainsKey(path))
                return cachedJObjects[path];

            // because StripHBSCommentsFromJSON is private, use Harmony to call the method
            var commentsStripped = Traverse.Create(typeof(JSONSerializationUtility)).Method("StripHBSCommentsFromJSON", File.ReadAllText(path)).GetValue<string>();
            if (commentsStripped == null)
                throw new Exception("StripHBSCommentsFromJSON returned null.");

            // add missing commas, this only fixes if there is a newline
            var rgx = new Regex(@"(\]|\}|""|[A-Za-z0-9])\s*\n\s*(\[|\{|"")", RegexOptions.Singleline);
            var commasAdded = rgx.Replace(commentsStripped, "$1,\n$2");

            cachedJObjects[path] = JObject.Parse(commasAdded);
            return cachedJObjects[path];
        }

        private static string InferIDFromFile(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        private static VersionManifestEntry FindEntry(string type, string id)
        {
            if (CustomResources.ContainsKey(type) && CustomResources[type].ContainsKey(id))
                return CustomResources[type][id];

            var modEntry = AddBTRLEntries.FindLast(x => x.Type == type && x.Id == id)?.GetVersionManifestEntry();
            if (modEntry != null)
                return modEntry;

            // if we're slating to remove an entry, then we don't want to return it here from the manifest
            return !RemoveBTRLEntries.Exists(entry => entry.Type == type && entry.Id == id)
                ? CachedVersionManifest.Find(entry => entry.Type == type && entry.Id == id)
                : null;
        }


        // READING MODDEFs AND LOAD/INIT/FINISH MODS
        private static bool LoadMod(ModDefEx modDef, out string reason)
        {
            Log($"{modDef.Name} {modDef.Version}");

            // read in custom resource types
            foreach (var customResourceType in modDef.CustomResourceTypes)
            {
                if (VANILLA_TYPES.Contains(customResourceType) || MODTEK_TYPES.Contains(customResourceType))
                {
                    Log($"\tWarning: {modDef.Name} has a custom resource type that has the same name as a vanilla/modtek resource type. Ignoring this type.");
                    continue;
                }

                if (!CustomResources.ContainsKey(customResourceType))
                    CustomResources.Add(customResourceType, new Dictionary<string, VersionManifestEntry>());
            }

            // expand the manifest (parses all JSON as well)
            var expandedManifest = ExpandManifest(modDef);
            if (expandedManifest == null)
            {
                reason = "Can't expand manifest";
                return false;
            }
            // load the mod assembly
            if (modDef.DLL != null && !LoadAssemblyAndCallInit(modDef))
            {
                reason = "Fail to call init method";
                return false;
            }
            // replace the manifest with our expanded manifest since we successfully got through loading the other stuff
            if (expandedManifest.Count > 0)
                Log($"\t{expandedManifest.Count} manifest entries");
            modDef.Manifest = expandedManifest;
            reason = "Success";
            return true;
        }

        private static List<ModEntry> ExpandManifest(ModDefEx modDef)
        {
            // note: if a JSON has errors, this mod will not load, since InferIDFromFile will throw from parsing the JSON
            var expandedManifest = new List<ModEntry>();

            if (modDef.LoadImplicitManifest && modDef.Manifest.All(x => Path.GetFullPath(Path.Combine(modDef.Directory, x.Path)) != Path.GetFullPath(Path.Combine(modDef.Directory, "StreamingAssets"))))
                modDef.Manifest.Add(new ModEntry("StreamingAssets", true));

            foreach (var modEntry in modDef.Manifest)
            {
                // handle prefabs; they have potential internal path to assetbundle
                if (modEntry.Type == "Prefab" && !string.IsNullOrEmpty(modEntry.AssetBundleName))
                {
                    if (!expandedManifest.Any(x => x.Type == "AssetBundle" && x.Id == modEntry.AssetBundleName))
                    {
                        Log($"\tError: {modDef.Name} has a Prefab '{modEntry.Id}' that's referencing an AssetBundle '{modEntry.AssetBundleName}' that hasn't been loaded. Put the assetbundle first in the manifest!");
                        return null;
                    }

                    modEntry.Id = Path.GetFileNameWithoutExtension(modEntry.Path);

                    if (!FileIsOnDenyList(modEntry.Path))
                        expandedManifest.Add(modEntry);

                    continue;
                }

                if (string.IsNullOrEmpty(modEntry.Path) && string.IsNullOrEmpty(modEntry.Type) && modEntry.Path != "StreamingAssets")
                {
                    Log($"\tError: {modDef.Name} has a manifest entry that is missing its path or type! Aborting load.");
                    return null;
                }

                if (!string.IsNullOrEmpty(modEntry.Type)
                    && !VANILLA_TYPES.Contains(modEntry.Type)
                    && !MODTEK_TYPES.Contains(modEntry.Type)
                    && !CustomResources.ContainsKey(modEntry.Type))
                {
                    Log($"\tError: {modDef.Name} has a manifest entry that has a type '{modEntry.Type}' that doesn't match an existing type and isn't declared in CustomResourceTypes");
                    return null;
                }

                var entryPath = Path.GetFullPath(Path.Combine(modDef.Directory, modEntry.Path));
                if (Directory.Exists(entryPath))
                {
                    // path is a directory, add all the files there
                    var files = Directory.GetFiles(entryPath, "*", SearchOption.AllDirectories).Where(filePath => !FileIsOnDenyList(filePath));
                    foreach (var filePath in files)
                    {
                        var path = Path.GetFullPath(filePath);
                        try
                        {
                            var childModEntry = new ModEntry(modEntry, path, InferIDFromFile(filePath));
                            expandedManifest.Add(childModEntry);
                        }
                        catch (Exception e)
                        {
                            LogException($"\tError: Canceling {modDef.Name} load!\n\tCaught exception reading file at {GetRelativePath(path, GameDirectory)}", e);
                            return null;
                        }
                    }
                }
                else if (File.Exists(entryPath) && !FileIsOnDenyList(entryPath))
                {
                    // path is a file, add the single entry
                    try
                    {
                        modEntry.Id = modEntry.Id ?? InferIDFromFile(entryPath);
                        modEntry.Path = entryPath;
                        expandedManifest.Add(modEntry);
                    }
                    catch (Exception e)
                    {
                        LogException($"\tError: Canceling {modDef.Name} load!\n\tCaught exception reading file at {GetRelativePath(entryPath, GameDirectory)}", e);
                        return null;
                    }
                }
                else if (modEntry.Path != "StreamingAssets")
                {
                    // path is not StreamingAssets and it's missing
                    Log($"\tWarning: Manifest specifies file/directory of {modEntry.Type} at path {modEntry.Path}, but it's not there. Continuing to load.");
                }
            }

            return expandedManifest;
        }

        private static bool LoadAssemblyAndCallInit(ModDefEx modDef)
        {
            var dllPath = Path.Combine(modDef.Directory, modDef.DLL);
            string typeName = null;
            var methodName = "Init";

            if (!File.Exists(dllPath))
            {
                Log($"\tError: DLL specified ({dllPath}), but it's missing! Aborting load.");
                return false;
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

            var assembly = AssemblyUtil.LoadDLL(dllPath);
            if (assembly == null)
            {
                Log($"\tError: Failed to load mod assembly at path {dllPath}.");
                return false;
            }

            var methods = AssemblyUtil.FindMethods(assembly, methodName, typeName);
            if (methods == null || methods.Length == 0)
            {
                Log($"\t\tError: Could not find any methods in assembly with name '{methodName}' and with type '{typeName ?? "not specified"}'");
                return false;
            }

            foreach (var method in methods)
            {
                var directory = modDef.Directory;
                var settings = modDef.Settings.ToString(Formatting.None);

                var parameterDictionary = new Dictionary<string, object>
                {
                    { "modDir", directory },
                    { "modDirectory", directory },
                    { "directory", directory },
                    { "modSettings", settings },
                    { "settings", settings },
                    { "settingsJson", settings },
                    { "settingsJSON", settings },
                    { "JSON", settings },
                    { "json", settings },
                };

                try
                {
                    if (AssemblyUtil.InvokeMethodByParameterNames(method, parameterDictionary))
                        continue;

                    if (AssemblyUtil.InvokeMethodByParameterTypes(method, new object[] { directory, settings }))
                        continue;
                }
                catch (Exception e)
                {
                    LogException($"\tError: While invoking '{method.DeclaringType?.Name}.{method.Name}', an exception occured", e);
                    return false;
                }

                Log($"\tError: Could not invoke method with name '{method.DeclaringType?.Name}.{method.Name}'");
                return false;
            }

            modDef.Assembly = assembly;

            if (!modDef.EnableAssemblyVersionCheck)
                TryResolveAssemblies.Add(assembly.GetName().Name, assembly);

            return true;
        }

        private static void CallFinishedLoadMethods()
        {
            var hasPrinted = false;
            var assemblyMods = ModLoadOrder.Where(name => ModDefs.ContainsKey(name) && ModDefs[name].Assembly != null).ToList();
            foreach (var assemblyMod in assemblyMods)
            {
                var modDef = ModDefs[assemblyMod];
                var methods = AssemblyUtil.FindMethods(modDef.Assembly, "FinishedLoading");

                if (methods == null || methods.Length == 0)
                    continue;

                if (!hasPrinted)
                {
                    Log("\nCalling FinishedLoading:");
                    hasPrinted = true;
                }

                var paramsDictionary = new Dictionary<string, object>
                {
                    { "loadOrder", new List<string>(ModLoadOrder) },
                };

                if (modDef.CustomResourceTypes.Count > 0)
                {
                    var customResources = new Dictionary<string, Dictionary<string, VersionManifestEntry>>();
                    foreach (var resourceType in modDef.CustomResourceTypes)
                        customResources.Add(resourceType, new Dictionary<string, VersionManifestEntry>(CustomResources[resourceType]));

                    paramsDictionary.Add("customResources", customResources);
                }

                foreach (var method in methods)
                {
                    if (!AssemblyUtil.InvokeMethodByParameterNames(method, paramsDictionary))
                        Log($"\tError: {modDef.Name}: Failed to invoke '{method.DeclaringType?.Name}.{method.Name}', parameter mismatch");
                }
            }
        }


        // ADDING/REMOVING CONTENT
        private static void AddModEntry(ModEntry modEntry)
        {
            if (modEntry.Path == null)
                return;

            // since we're adding a new entry here, we want to remove anything that would remove it again after the fact
            if (RemoveBTRLEntries.RemoveAll(entry => entry.Id == modEntry.Id && entry.Type == modEntry.Type) > 0)
                Log($"\t\t{modEntry.Id} ({modEntry.Type}) -- this entry replaced an entry that was slated to be removed. Removed the removal.");

            if (CustomResources.ContainsKey(modEntry.Type))
            {
                Log($"\tAdd/Replace (CustomResource): \"{GetRelativePath(modEntry.Path, ModsDirectory)}\" ({modEntry.Type})");
                CustomResources[modEntry.Type][modEntry.Id] = modEntry.GetVersionManifestEntry();
                return;
            }

            VersionManifestAddendum addendum = null;
            if (!string.IsNullOrEmpty(modEntry.AddToAddendum))
            {
                addendum = CachedVersionManifest.GetAddendumByName(modEntry.AddToAddendum);

                if (addendum == null)
                {
                    Log($"\tWarning: Cannot add {modEntry.Id} to {modEntry.AddToAddendum} because addendum doesn't exist in the manifest.");
                    return;
                }
            }

            // special handling for particular types
            switch (modEntry.Type)
            {
                case "AssetBundle":
                    ModAssetBundlePaths[modEntry.Id] = modEntry.Path;
                    break;
            }

            // add to addendum instead of adding to manifest
            if (addendum != null)
                Log($"\tAdd/Replace: \"{GetRelativePath(modEntry.Path, ModsDirectory)}\" ({modEntry.Type}) [{addendum.Name}]");
            else
                Log($"\tAdd/Replace: \"{GetRelativePath(modEntry.Path, ModsDirectory)}\" ({modEntry.Type})");

            // entries in AddBTRLEntries will be added to game through patch in Patches\BattleTechResourceLocator
            AddBTRLEntries.Add(modEntry);
        }

        private static bool AddModEntryToDB(MetadataDatabase db, DBCache dbCache, string absolutePath, string typeStr)
        {
            if (Path.GetExtension(absolutePath)?.ToLowerInvariant() != ".json")
                return false;

            var type = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), typeStr);
            var relativePath = GetRelativePath(absolutePath, GameDirectory);

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
                    var writeTime = File.GetLastWriteTimeUtc(absolutePath);
                    if (!dbCache.Entries.ContainsKey(relativePath) || dbCache.Entries[relativePath] != writeTime)
                    {
                        try
                        {
                            VersionManifestHotReload.InstantiateResourceAndUpdateMDDB(type, absolutePath, db);

                            // don't write game files to the dbCache, since they're assumed to be default in the db
                            if (!absolutePath.Contains(StreamingAssetsDirectory))
                                dbCache.Entries[relativePath] = writeTime;

                            return true;
                        }
                        catch (Exception e)
                        {
                            LogException($"\tError: Add to DB failed for {Path.GetFileName(absolutePath)}, exception caught:", e);
                            return false;
                        }
                    }
                    break;
            }

            return false;
        }

        private static void AddMerge(string type, string id, string path)
        {
            if (!merges.ContainsKey(type))
                merges[type] = new Dictionary<string, List<string>>();

            if (!merges[type].ContainsKey(id))
                merges[type][id] = new List<string>();

            if (merges[type][id].Contains(path))
                return;

            merges[type][id].Add(path);
        }

        private static bool RemoveEntry(string id, TypeCache typeCache)
        {
            var removedEntry = false;

            var containingCustomTypes = CustomResources.Where(pair => pair.Value.ContainsKey(id)).ToList();
            foreach (var pair in containingCustomTypes)
            {
                Log($"\tRemove: \"{pair.Value[id].Id}\" ({pair.Value[id].Type}) - Custom Resource");
                pair.Value.Remove(id);
                removedEntry = true;
            }

            var modEntries = AddBTRLEntries.FindAll(entry => entry.Id == id);
            foreach (var modEntry in modEntries)
            {
                Log($"\tRemove: \"{modEntry.Id}\" ({modEntry.Type}) - Mod Entry");
                AddBTRLEntries.Remove(modEntry);
                removedEntry = true;
            }

            var vanillaEntries = CachedVersionManifest.FindAll(entry => entry.Id == id);
            foreach (var vanillaEntry in vanillaEntries)
            {
                Log($"\tRemove: \"{vanillaEntry.Id}\" ({vanillaEntry.Type}) - Vanilla Entry");
                RemoveBTRLEntries.Add(vanillaEntry);
                removedEntry = true;
            }

            var types = typeCache.GetTypes(id, CachedVersionManifest);
            foreach (var type in types)
            {
                if (!merges.ContainsKey(type) || !merges[type].ContainsKey(id))
                    continue;

                Log($"\t\tAlso removing JSON merges for {id} ({type})");
                merges[type].Remove(id);
            }

            return removedEntry;
        }

        private static void RemoveMerge(string type, string id)
        {
            if (!merges.ContainsKey(type) || !merges[type].ContainsKey(id))
                return;

            merges[type].Remove(id);
            Log($"\t\tHad merges for {id} but had to toss, since original file is being replaced");
        }


        // LOADING MODS WORK
        private static void LoadMods()
        {
            CachedVersionManifest = VersionManifestUtilities.LoadDefaultManifest();

            // setup custom resources for ModTek types with fake VersionManifestEntries
            CustomResources.Add("Video", new Dictionary<string, VersionManifestEntry>());
            CustomResources.Add("SoundBank", new Dictionary<string, VersionManifestEntry>());

            CustomResources.Add("DebugSettings", new Dictionary<string, VersionManifestEntry>());
            CustomResources["DebugSettings"]["settings"] = new VersionManifestEntry("settings", Path.Combine(StreamingAssetsDirectory, DebugSettingsPath), "DebugSettings", DateTime.Now, "1");

            CustomResources.Add("GameTip", new Dictionary<string, VersionManifestEntry>());
            CustomResources["GameTip"]["general"] = new VersionManifestEntry("general", Path.Combine(StreamingAssetsDirectory, Path.Combine("GameTips", "general.txt")), "GameTip", DateTime.Now, "1");
            CustomResources["GameTip"]["combat"] = new VersionManifestEntry("combat", Path.Combine(StreamingAssetsDirectory, Path.Combine("GameTips", "combat.txt")), "GameTip", DateTime.Now, "1");
            CustomResources["GameTip"]["lore"] = new VersionManifestEntry("lore", Path.Combine(StreamingAssetsDirectory, Path.Combine("GameTips", "lore.txt")), "GameTip", DateTime.Now, "1");
            CustomResources["GameTip"]["sim"] = new VersionManifestEntry("sim", Path.Combine(StreamingAssetsDirectory, Path.Combine("GameTips", "sim.txt")), "GameTip", DateTime.Now, "1");

            ProgressPanel.SubmitWork(InitModsLoop);
            ProgressPanel.SubmitWork(HandleModManifestsLoop);
            ProgressPanel.SubmitWork(MergeFilesLoop);
            ProgressPanel.SubmitWork(AddToDBLoop);
            ProgressPanel.SubmitWork(GatherDependencyTreeLoop);
            ProgressPanel.SubmitWork(FinishLoop);
        }

        private static IEnumerator<ProgressReport> GatherDependencyTreeLoop()
        {
            yield return new ProgressReport(0, "Gathering dependencies trees", "");
            if(allModDefs.Count == 0)
            {
                yield break;
            }
            int progeress = 0;
            foreach(var mod in allModDefs)
            {
                ++progeress;
                yield return new ProgressReport(progeress / ((float)allModDefs.Count), $"Gather depends on me", mod.Key, true);
                foreach(string depname in mod.Value.DependsOn)
                {
                    if (allModDefs.ContainsKey(depname)) { if(allModDefs[depname].DependsOnMe.Contains(mod.Value) == false) { allModDefs[depname].DependsOnMe.Add(mod.Value); }; };
                }
            }
            progeress = 0;
            foreach (var mod in allModDefs)
            {
                ++progeress;
                yield return new ProgressReport(progeress / ((float)allModDefs.Count), $"Gather disable influence tree", mod.Key, true);
                mod.Value.GatherAffectingOfflineRec();
            }
            progeress = 0;
            foreach (var mod in allModDefs)
            {
                ++progeress;
                yield return new ProgressReport(progeress / ((float)allModDefs.Count), $"Gather enable influence tree", mod.Key, true);
                mod.Value.GatherAffectingOnline();
            }
        }

        private static void GatherAffectingOfflineRec(this ModDefEx mod)
        {
            Dictionary<ModDefEx, bool> deps = new Dictionary<ModDefEx, bool>();
            Log("Gathering "+mod.Name+"->Disable influence. My state:"+mod.Enabled+" fail:"+(mod.LoadFail?mod.FailReason:"no"));
            GatherAffectingOfflineRec(mod, ref deps, 1);
            mod.AffectingOffline = deps;
        }

        private static void GatherAffectingOfflineRec(this ModDefEx mod,ref Dictionary<ModDefEx,bool> deps, int level)
        {
            foreach(var dmod in mod.DependsOnMe)
            {
                if (deps.ContainsKey(dmod) == false) {
                    string i = new string(' ', level);
                    Log(i + dmod.Name + " state:" + dmod.Enabled + " fail:" + (dmod.LoadFail ? dmod.FailReason : "no"));
                    deps.Add(dmod, false); GatherAffectingOfflineRec(dmod, ref deps,level+1);
                };
            }
        }

        private static void GatherAffectingOnlineRec(this ModDefEx mod, ref Dictionary<ModDefEx, bool> deps, int level)
        {
            foreach (string dep in mod.DependsOn)
            {
                string i = new string(' ', level);
                if (allModDefs.ContainsKey(dep) == false) {
                    Log(i + dep + " state:Absent!");
                    continue;
                }
                ModDefEx dmod = allModDefs[dep];
                if (deps.ContainsKey(dmod) == false) {
                    Log(i + dmod.Name + " state:" + dmod.Enabled + " fail:" + (dmod.LoadFail ? dmod.FailReason : "no"));
                    deps.Add(dmod,true); GatherAffectingOnlineRec(dmod,ref deps, level+1);
                }
            }
        }

        private static void GatherConflicts(this ModDefEx mod, ref Dictionary<ModDefEx, bool> deps)
        {
            foreach(string dep in mod.ConflictsWith)
            {
                if (allModDefs.ContainsKey(dep) == false) {
                    Log("  due to "+mod.Name+" with "+dep+" state:Abcent");
                    continue;
                }
                ModDefEx dmod = allModDefs[dep];
                Log("  due to " + mod.Name + " with " + dmod.Name + " state:" + dmod.Enabled + " fail:" + (dmod.LoadFail ? dmod.FailReason : "no"));
                if (deps.ContainsKey(dmod) == false) {
                    deps.Add(dmod, false);
                }
            }
        }
        private static void GatherAffectingOnline(this ModDefEx mod)
        {
            Dictionary<ModDefEx, bool> deps = new Dictionary<ModDefEx, bool>();
            Log("Gathering " + mod.Name + "->Enable influence. My state:" + mod.Enabled + " fail:" + (mod.LoadFail ? mod.FailReason : "no"));
            Log(" I'm depends on:");
            GatherAffectingOnlineRec(mod, ref deps, 2);
            HashSet<ModDefEx> conflicts = deps.Keys.ToHashSet();
            Log(" Conflicts:");
            foreach (ModDefEx cmod in conflicts)
            {
                GatherConflicts(cmod, ref deps);
            }
            mod.AffectingOnline = deps;
        }

        private static IEnumerator<ProgressReport> InitModsLoop()
        {
            yield return new ProgressReport(1, "Initializing Mods", "");

            // find all sub-directories that have a mod.json file
            var modDirectories = Directory.GetDirectories(ModsDirectory).Where(x => File.Exists(Path.Combine(x, MOD_JSON_NAME))).ToArray();
            //var modFiles = Directory.GetFiles(ModsDirectory, MOD_JSON_NAME, SearchOption.AllDirectories);

            if (modDirectories.Length == 0)
            {
                Log("No ModTek-compatible mods found.");
                yield break;
            }

            // create ModDef objects for each mod.json file
            foreach (var modDirectory in modDirectories)
            {
                ModDefEx modDef;
                //var modDirectory = Path.GetDirectoryName(modFile);
                var modDefPath = Path.Combine(modDirectory, MOD_JSON_NAME);
                try
                {
                    modDef = ModDefEx.CreateFromPath(modDefPath);
                    if (modDef.Name == MODTEK_DEF_NAME) { modDef = SettingsDef; }
                }
                catch (Exception e)
                {
                    FailedToLoadMods.Add(GetRelativePath(modDirectory, ModsDirectory));
                    LogException($"Error: Caught exception while parsing {MOD_JSON_NAME} at path {modDefPath}", e);
                    continue;
                }

                if (allModDefs.ContainsKey(modDef.Name) == false) { allModDefs.Add(modDef.Name,modDef); } else
                {
                    int counter = 0;
                    string tmpname = modDef.Name;
                    do
                    {
                        ++counter;
                        tmpname = modDef.Name + "{dublicate " + counter + "}";
                    } while (allModDefs.ContainsKey(tmpname) == true);
                    modDef.Name = tmpname;
                    modDef.Enabled = false;
                    modDef.LoadFail = true;
                    modDef.FailReason = "dublicate";
                    //modDef.Description = "Dublicate";
                    allModDefs.Add(modDef.Name, modDef);
                    continue;
                }

                if (!modDef.ShouldTryLoad(ModDefs.Keys.ToList(), out var reason, out bool shouldAddToList))
                {
                    Log($"Not loading {modDef.Name} because {reason}");
                    if (!modDef.IgnoreLoadFailure)
                    {
                        FailedToLoadMods.Add(modDef.Name);
                        if (allModDefs.ContainsKey(modDef.Name)) {
                            allModDefs[modDef.Name].LoadFail = true;
                            modDef.FailReason = reason;
                            //allModDefs[modDef.Name].Description = reason;
                        }
                    }
                    continue;
                }
                ModDefs.Add(modDef.Name, modDef);
            }

            // get a load order and remove mods that won't be loaded
            ModLoadOrder = LoadOrder.CreateLoadOrder(ModDefs, out var notLoaded, LoadOrder.FromFile(LoadOrderPath));
            foreach (var modName in notLoaded)
            {
                var modDef = ModDefs[modName];
                ModDefs.Remove(modName);
                if (modDef.IgnoreLoadFailure) { continue; }
                if (allModDefs.ContainsKey(modName))
                {
                    allModDefs[modName].LoadFail = true;
                    allModDefs[modName].FailReason = $"Warning: Will not load {modName} because it's lacking a dependency or has a conflict.";
                }
                Log($"Warning: Will not load {modName} because it's lacking a dependency or has a conflict.");
                FailedToLoadMods.Add(modName);
            }

            // try loading each mod
            var numModsLoaded = 0;
            Log("");
            foreach (var modName in ModLoadOrder)
            {
                var modDef = ModDefs[modName];

                if (modDef.DependsOn.Intersect(FailedToLoadMods).Any())
                {
                    ModDefs.Remove(modName);
                    if (!modDef.IgnoreLoadFailure)
                    {
                        Log($"Warning: Skipping load of {modName} because one of its dependencies failed to load.");
                        if (allModDefs.ContainsKey(modName)) {
                            allModDefs[modName].LoadFail = true;
                            allModDefs[modName].FailReason = $"Warning: Skipping load of {modName} because one of its dependencies failed to load.";
                        }
                        FailedToLoadMods.Add(modName);
                    }
                    continue;
                }

                yield return new ProgressReport(numModsLoaded++ / ((float)ModLoadOrder.Count), "Initializing Mods", $"{modDef.Name} {modDef.Version}", true);

                try
                {
                    if (!LoadMod(modDef, out string reason))
                    {
                        ModDefs.Remove(modName);

                        if (!modDef.IgnoreLoadFailure)
                        {
                            FailedToLoadMods.Add(modName);
                            if (allModDefs.ContainsKey(modName))
                            {
                                allModDefs[modName].LoadFail = true;
                                allModDefs[modName].FailReason = reason;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    ModDefs.Remove(modName);

                    if (modDef.IgnoreLoadFailure)
                        continue;

                    LogException($"Error: Tried to load mod: {modDef.Name}, but something went wrong. Make sure all of your JSON is correct!", e);
                    FailedToLoadMods.Add(modName);
                    if (allModDefs.ContainsKey(modName))
                    {
                        allModDefs[modName].LoadFail = true;
                        allModDefs[modName].FailReason = "Error: Tried to load mod: " + modDef.Name +", but something went wrong. Make sure all of your JSON is correct!" + e.ToString();
                    }
                }
            }
        }

        private static IEnumerator<ProgressReport> HandleModManifestsLoop()
        {
            // there are no mods loaded, just return
            if (ModLoadOrder == null || ModLoadOrder.Count == 0)
                yield break;

            Log("\nAdding Mod Content...");
            var typeCache = new TypeCache(TypeCachePath);
            typeCache.UpdateToIDBased();
            Log("");

            // progress panel setup
            var entryCount = 0;
            var numEntries = 0;
            ModDefs.Do(entries => numEntries += entries.Value.Manifest.Count);

            var manifestMods = ModLoadOrder.Where(name => ModDefs.ContainsKey(name) && (ModDefs[name].Manifest.Count > 0 || ModDefs[name].RemoveManifestEntries.Count > 0)).ToList();
            foreach (var modName in manifestMods)
            {
                var modDef = ModDefs[modName];

                Log($"{modName}:");
                yield return new ProgressReport(entryCount / ((float)numEntries), $"Loading {modName}", "", true);

                foreach (var modEntry in modDef.Manifest)
                {
                    yield return new ProgressReport(entryCount++ / ((float)numEntries), $"Loading {modName}", modEntry.Id);

                    // type being null means we have to figure out the type from the path (StreamingAssets)
                    if (modEntry.Type == null)
                    {
                        var relativePath = GetRelativePath(modEntry.Path, Path.Combine(modDef.Directory, "StreamingAssets"));

                        if (relativePath == DebugSettingsPath)
                            modEntry.Type = "DebugSettings";
                    }

                    // type *still* being null means that this is an "non-special" case, i.e. it's in the manifest
                    if (modEntry.Type == null)
                    {
                        var relativePath = GetRelativePath(modEntry.Path, Path.Combine(modDef.Directory, "StreamingAssets"));
                        var fakeStreamingAssetsPath = Path.GetFullPath(Path.Combine(StreamingAssetsDirectory, relativePath));
                        if (!File.Exists(fakeStreamingAssetsPath))
                        {
                            Log($"\tWarning: Could not find a file at {fakeStreamingAssetsPath} for {modName} {modEntry.Id}. NOT LOADING THIS FILE");
                            continue;
                        }

                        var types = typeCache.GetTypes(modEntry.Id, CachedVersionManifest);
                        if (types == null)
                        {
                            Log($"\tWarning: Could not find an existing VersionManifest entry for {modEntry.Id}. Is this supposed to be a new entry? Don't put new entries in StreamingAssets!");
                            continue;
                        }

                        // this is getting merged later and then added to the BTRL entries then
                        // StreamingAssets don't get default appendText
                        if (Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".json" && modEntry.ShouldMergeJSON)
                        {
                            // this assumes that vanilla .json can only have a single type
                            // typeCache will always contain this path
                            modEntry.Type = typeCache.GetTypes(modEntry.Id)[0];
                            AddMerge(modEntry.Type, modEntry.Id, modEntry.Path);
                            Log($"\tMerge: \"{GetRelativePath(modEntry.Path, ModsDirectory)}\" ({modEntry.Type})");
                            continue;
                        }

                        foreach (var type in types)
                        {
                            var subModEntry = new ModEntry(modEntry, modEntry.Path, modEntry.Id);
                            subModEntry.Type = type;
                            AddModEntry(subModEntry);
                            RemoveMerge(type, modEntry.Id);
                        }

                        continue;
                    }

                    // special handling for types
                    switch (modEntry.Type)
                    {
                        case "AdvancedJSONMerge":
                            {
                                var advancedJSONMerge = AdvancedJSONMerge.FromFile(modEntry.Path);

                                if (!string.IsNullOrEmpty(advancedJSONMerge.TargetID) && advancedJSONMerge.TargetIDs == null)
                                    advancedJSONMerge.TargetIDs = new List<string> { advancedJSONMerge.TargetID };

                                if (advancedJSONMerge.TargetIDs == null || advancedJSONMerge.TargetIDs.Count == 0)
                                {
                                    Log($"\tError: AdvancedJSONMerge: \"{GetRelativePath(modEntry.Path, ModsDirectory)}\" didn't target any IDs. Skipping this merge.");
                                    continue;
                                }

                                foreach (var id in advancedJSONMerge.TargetIDs)
                                {
                                    var type = advancedJSONMerge.TargetType;
                                    if (string.IsNullOrEmpty(type))
                                    {
                                        var types = typeCache.GetTypes(id, CachedVersionManifest);
                                        if (types == null || types.Count == 0)
                                        {
                                            Log($"\tError: AdvancedJSONMerge: \"{GetRelativePath(modEntry.Path, ModsDirectory)}\" could not resolve type for ID: {id}. Skipping this merge");
                                            continue;
                                        }

                                        // assume that only a single type
                                        type = types[0];
                                    }

                                    var entry = FindEntry(type, id);
                                    if (entry == null)
                                    {
                                        Log($"\tError: AdvancedJSONMerge: \"{GetRelativePath(modEntry.Path, ModsDirectory)}\" could not find entry {id} ({type}). Skipping this merge");
                                        continue;
                                    }

                                    AddMerge(type, id, modEntry.Path);
                                    Log($"\tAdvancedJSONMerge: \"{GetRelativePath(modEntry.Path, ModsDirectory)}\" targeting '{id}' ({type})");
                                }

                                continue;
                            }
                    }

                    // non-StreamingAssets json merges
                    if (Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".json" && modEntry.ShouldMergeJSON
                        || (Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".txt" || Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".csv") && modEntry.ShouldAppendText)
                    {
                        // have to find the original path for the manifest entry that we're merging onto
                        var matchingEntry = FindEntry(modEntry.Type, modEntry.Id);

                        if (matchingEntry == null)
                        {
                            Log($"\tWarning: Could not find an existing VersionManifest entry for {modEntry.Id}!");
                            continue;
                        }

                        // this assumes that .json can only have a single type
                        typeCache.TryAddType(modEntry.Id, modEntry.Type);
                        Log($"\tMerge: \"{GetRelativePath(modEntry.Path, ModsDirectory)}\" ({modEntry.Type})");
                        AddMerge(modEntry.Type, modEntry.Id, modEntry.Path);
                        continue;
                    }

                    typeCache.TryAddType(modEntry.Id, modEntry.Type);
                    AddModEntry(modEntry);
                    RemoveMerge(modEntry.Type, modEntry.Id);
                }

                foreach (var removeID in ModDefs[modName].RemoveManifestEntries)
                {
                    if (!RemoveEntry(removeID, typeCache))
                    {
                        Log($"\tWarning: Could not find manifest entries for {removeID} to remove them. Skipping.");
                    }
                }
            }

            typeCache.ToFile(TypeCachePath);
        }

        private static IEnumerator<ProgressReport> MergeFilesLoop()
        {
            // there are no mods loaded, just return
            if (ModLoadOrder == null || ModLoadOrder.Count == 0)
                yield break;

            // perform merges into cache
            Log("\nDoing merges...");
            yield return new ProgressReport(1, "Merging", "", true);

            var mergeCache = MergeCache.FromFile(MergeCachePath);
            mergeCache.UpdateToRelativePaths();

            // progress panel setup
            var mergeCount = 0;
            var numEntries = 0;
            merges.Do(pair => numEntries += pair.Value.Count);

            foreach (var type in merges.Keys)
            {
                foreach (var id in merges[type].Keys)
                {
                    var existingEntry = FindEntry(type, id);
                    if (existingEntry == null)
                    {
                        Log($"\tWarning: Have merges for {id} but cannot find an original file! Skipping.");
                        continue;
                    }

                    var originalPath = Path.GetFullPath(existingEntry.FilePath);
                    var mergePaths = merges[type][id];

                    if (!mergeCache.HasCachedEntry(originalPath, mergePaths))
                        yield return new ProgressReport(mergeCount++ / ((float)numEntries), "Merging", id);

                    var cachePath = mergeCache.GetOrCreateCachedEntry(originalPath, mergePaths);

                    // something went wrong (the parent json prob had errors)
                    if (cachePath == null)
                        continue;

                    var cacheEntry = new ModEntry(cachePath)
                    {
                        ShouldAppendText = false,
                        ShouldMergeJSON = false,
                        Type = existingEntry.Type,
                        Id = id
                    };

                    AddModEntry(cacheEntry);
                }
            }

            mergeCache.ToFile(MergeCachePath);
        }

        private static IEnumerator<ProgressReport> AddToDBLoop()
        {
            // there are no mods loaded, just return
            if (ModLoadOrder == null || ModLoadOrder.Count == 0)
                yield break;

            Log("\nSyncing Database...");
            yield return new ProgressReport(1, "Syncing Database", "", true);

            var dbCache = new DBCache(DBCachePath, MDDBPath, ModMDDBPath);
            dbCache.UpdateToRelativePaths();

            // since DB instance is read at type init, before we patch the file location
            // need re-init the mddb to read from the proper modded location
            var mddbTraverse = Traverse.Create(typeof(MetadataDatabase));
            mddbTraverse.Field("instance").SetValue(null);
            mddbTraverse.Method("InitInstance").GetValue();

            // check if files removed from DB cache
            var shouldWriteDB = false;
            var shouldRebuildDB = false;
            var replacementEntries = new List<VersionManifestEntry>();
            var removeEntries = new List<string>();
            foreach (var path in dbCache.Entries.Keys)
            {
                var absolutePath = ResolvePath(path, GameDirectory);

                // check if the file in the db cache is still used
                if (AddBTRLEntries.Exists(x => x.Path == absolutePath))
                    continue;

                Log($"\tNeed to remove DB entry from file in path: {path}");

                // file is missing, check if another entry exists with same filename in manifest or in BTRL entries
                var fileName = Path.GetFileName(path);
                var existingEntry = AddBTRLEntries.FindLast(x => Path.GetFileName(x.Path) == fileName)?.GetVersionManifestEntry()
                    ?? CachedVersionManifest.Find(x => Path.GetFileName(x.FilePath) == fileName);

                // TODO: DOES NOT HANDLE CASE WHERE REMOVING VANILLA CONTENT IN DB

                if (existingEntry == null)
                {
                    Log("\t\tHave to rebuild DB, no existing entry in VersionManifest matches removed entry");
                    shouldRebuildDB = true;
                    break;
                }

                replacementEntries.Add(existingEntry);
                removeEntries.Add(path);
            }

            // add removed entries replacements to db
            if (!shouldRebuildDB)
            {
                // remove old entries
                foreach (var removeEntry in removeEntries)
                    dbCache.Entries.Remove(removeEntry);

                foreach (var replacementEntry in replacementEntries)
                {
                    if (AddModEntryToDB(MetadataDatabase.Instance, dbCache, Path.GetFullPath(replacementEntry.FilePath), replacementEntry.Type))
                    {
                        Log($"\t\tReplaced DB entry with an existing entry in path: {GetRelativePath(replacementEntry.FilePath, GameDirectory)}");
                        shouldWriteDB = true;
                    }
                }
            }

            // if an entry has been removed and we cannot find a replacement, have to rebuild the mod db
            if (shouldRebuildDB)
                dbCache = new DBCache(null, MDDBPath, ModMDDBPath);

            Log($"\nAdding dynamic enums:");
            var addCount = 0;
            List<ModDefEx> mods = new List<ModDefEx>();
            foreach (string modname in ModLoadOrder)
            {
                if (ModTek.ModDefs.ContainsKey(modname) == false) { continue; }
                mods.Add(ModTek.ModDefs[modname]);
            }
            foreach (ModDefEx moddef in mods)
            {
                if (moddef.DataAddendumEntries.Count != 0)
                {
                    Log($"{moddef.Name}:");
                    foreach (DataAddendumEntry dataAddendumEntry in moddef.DataAddendumEntries)
                    {
                        if (ModTek.LoadDataAddendum(dataAddendumEntry, moddef.Directory)) { shouldWriteDB = true; }
                        yield return new ProgressReport(addCount / ((float)mods.Count), "Populating Dynamic Enums", moddef.Name);
                    }
                }
                ++addCount;
            }
            // add needed files to db
            addCount = 0;
            foreach (var modEntry in AddBTRLEntries)
            {
                if (modEntry.AddToDB && AddModEntryToDB(MetadataDatabase.Instance, dbCache, modEntry.Path, modEntry.Type))
                {
                    yield return new ProgressReport(addCount / ((float)AddBTRLEntries.Count), "Populating Database", modEntry.Id);
                    Log($"\tAdded/Updated {modEntry.Id} ({modEntry.Type})");
                    shouldWriteDB = true;
                }
                addCount++;
            }

            //ModLoadOrder.Count;


            dbCache.ToFile(DBCachePath);

            if (shouldWriteDB)
            {
                yield return new ProgressReport(1, "Writing Database", "", true);
                Log("Writing DB");
                MetadataDatabase.Instance.WriteInMemoryDBToDisk();
            }
        }

        private static IEnumerator<ProgressReport> FinishLoop()
        {
            // "Loop"
            yield return new ProgressReport(1, "Finishing Up", "", true);
            Log("\nFinishing Up");

            if (CustomResources["DebugSettings"]["settings"].FilePath != Path.Combine(StreamingAssetsDirectory, DebugSettingsPath))
                DebugBridge.LoadSettings(CustomResources["DebugSettings"]["settings"].FilePath);

            if (ModLoadOrder != null && ModLoadOrder.Count > 0)
            {
                CallFinishedLoadMethods();
                PrintHarmonySummary(HarmonySummaryPath);
                LoadOrder.ToFile(ModLoadOrder, LoadOrderPath);
            }

            Config?.ToFile(ConfigPath);

            Finish();
        }
        public static bool LoadDataAddendum(DataAddendumEntry dataAddendumEntry, string modDefDirectory)
        {
            try
            {
                System.Type type = typeof(FactionEnumeration).Assembly.GetType(dataAddendumEntry.name);
                if (type == (System.Type)null)
                {
                    Log("\tError: Could not find DataAddendum class named " + dataAddendumEntry.name);
                    return false;
                }
                else
                {
                    PropertyInfo property = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
                    if (property == (PropertyInfo)null)
                    {
                        Log("\tError: Could not find static method [Instance] on class named [" + dataAddendumEntry.name + "]");
                        return false;
                    }
                    else
                    {
                        object bdataAddendum = property.GetValue((object)null);
                        //BattleTech.ModSupport.IDataAddendum dataAddendum = bdataAddendum as BattleTech.ModSupport.IDataAddendum;
                        //Enumeration<type.GetType()> dataAddendum = property.GetValue((object)null) as BattleTech.ModSupport.IDataAddendum;
                        //Log("\tError: Class does not implement interface [IDataAddendum] on class named [" + dataAddendumEntry.name + "]");
                        PropertyInfo pCachedEnumerationValueList = type.BaseType.GetProperty("CachedEnumerationValueList");
                        if (pCachedEnumerationValueList == null)
                        {
                            Log("\tError: Class does not implement property CachedEnumerationValueList property on class named [" + dataAddendumEntry.name + "]");
                            return false;
                        }
                        else
                        {
                            IJsonTemplated jdataAddEnum = bdataAddendum as IJsonTemplated;
                            if (jdataAddEnum == null)
                            {
                                Log("\tError: not IJsonTemplated [" + dataAddendumEntry.name + "]");
                                return false;
                            }
                            else
                            {
                                string fileData = File.ReadAllText(Path.Combine(modDefDirectory, dataAddendumEntry.path));
                                jdataAddEnum.FromJSON(fileData);
                                IList enumList = pCachedEnumerationValueList.GetValue(bdataAddendum, null) as IList;
                                if (enumList == null)
                                {
                                    Log("\tError: Can't get CachedEnumerationValueList from [" + dataAddendumEntry.name + "]");
                                    return false;
                                }
                                else
                                {
                                    bool needFlush = false;
                                    Log("\tLoading values [" + dataAddendumEntry.name + "]");
                                    for (int index = 0; index < enumList.Count; ++index)
                                    {
                                        EnumValue val = enumList[index] as EnumValue;
                                        if (val == null) { continue; };
                                        if (val.GetType() == typeof(FactionValue))
                                        {
                                            MetadataDatabase.Instance.InsertOrUpdateFactionValue(val as FactionValue);
                                            Log("\t\tAddind FactionValue to db [" + val.Name + ":" + val.ID + "]");
                                            needFlush = true;
                                        }
                                        else
                                        if (val.GetType() == typeof(WeaponCategoryValue))
                                        {
                                            MetadataDatabase.Instance.InsertOrUpdateWeaponCategoryValue(val as WeaponCategoryValue);
                                            Log("\t\tAddind WeaponCategoryValue to db [" + val.Name + ":" + val.ID + "]");
                                            needFlush = true;
                                        }
                                        else
                                        if(val.GetType() == typeof(AmmoCategoryValue))
                                        {
                                            MetadataDatabase.Instance.InsertOrUpdateAmmoCategoryValue(val as AmmoCategoryValue);
                                            Log("\t\tAddind AmmoCategoryValue to db [" + val.Name + ":" + val.ID + "]");
                                            needFlush = true;
                                        }
                                        else
                                        if (val.GetType() == typeof(ContractTypeValue))
                                        {
                                            MetadataDatabase.Instance.InsertOrUpdateContractTypeValue(val as ContractTypeValue);
                                            Log("\t\tAddind ContractTypeValue to db [" + val.Name + ":" + val.ID + "]");
                                            needFlush = true;
                                        }
                                        else
                                        {
                                            Log("\t\tUnknown enum type");
                                            break;
                                        }
                                    }
                                    if (needFlush)
                                    {
                                        Log("\tLog: DataAddendum successfully loaded name[" + dataAddendumEntry.name + "] path[" + dataAddendumEntry.path + "]");
                                    }
                                    return needFlush;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("\tException: Exception caught while processing DataAddendum [" + dataAddendumEntry.name + "]");
                Log(ex.ToString());
                return false;
            }
        }
    }
}
