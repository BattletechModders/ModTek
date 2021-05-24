using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS;
using ModTek.Caches;
using ModTek.RuntimeLog;
using ModTek.UI;
using ModTek.Util;
using Newtonsoft.Json;
using SVGImporter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ModTek.AdvMerge;
using ModTek.CustomTypes;
using ModTek.Manifest;
using ModTek.Misc;
using ModTek.Mods;
using ModTek.SoundBanks;
using static ModTek.Util.Logger;

namespace ModTek
{
    internal static class ModTek
    {
        internal static bool HasLoaded { get; private set; }

        // TODO Move
        // game paths/directories
        internal static ModDefEx SettingsDef { get; private set; }

        internal static bool Enabled => SettingsDef.Enabled;

        internal const string MODTEK_DEF_NAME = "ModTek";
        internal const string MOD_STATE_JSON_NAME = "modstate.json";

        private static HashSet<string> systemIcons = new();

        internal static bool isInSystemIcons(string id)
        {
            return systemIcons.Contains(id);
        }

        // special StreamingAssets relative directories

        // internal temp structures
        private static System.Diagnostics.Stopwatch stopwatch = new();
        private static Dictionary<string, Dictionary<string, List<string>>> merges = new();
        private static HashSet<string> BTRLEntriesPathes;

        // internal structures
        internal static Configuration Config;
        private static List<string> ModLoadOrder;
        internal static Dictionary<string, ModDefEx> ModDefs = new();
        internal static Dictionary<string, ModDefEx> allModDefs = new();
        internal static HashSet<string> FailedToLoadMods { get; } = new();
        internal static Dictionary<string, Assembly> TryResolveAssemblies = new();

        // the end result of loading mods, these are used to push into game data through patches
        internal static VersionManifest CachedVersionManifest;
        internal static List<ModEntry> AddBTRLEntries = new();
        internal static List<VersionManifestEntry> RemoveBTRLEntries = new();
        internal static Dictionary<string, Dictionary<string, VersionManifestEntry>> CustomResources = new();
        internal static Dictionary<string, string> ModAssetBundlePaths { get; } = new();

        internal static HashSet<ModEntry> CustomTags = new();
        internal static HashSet<ModEntry> CustomTagSets = new();

        // INITIALIZATION (called by injected code)
        public static void Init()
        {
            if (HasLoaded)
            {
                return;
            }

            stopwatch.Start();

            if (!FilePaths.SetupPaths())
            {
                return;
            }

            // creates the directories above it as well
            Directory.CreateDirectory(FilePaths.CacheDirectory);
            Directory.CreateDirectory(FilePaths.DatabaseDirectory);

            var versionString = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            RLog.InitLog(FilePaths.TempModTekDirectory, true);
            RLog.M.TWL(0, "Init ModTek version " + Assembly.GetExecutingAssembly().GetName().Version);
            if (File.Exists(FilePaths.ChangedFlagPath))
            {
                File.Delete(FilePaths.ChangedFlagPath);
                FileUtils.CleanModTekTempDir(new DirectoryInfo(FilePaths.TempModTekDirectory));
                Directory.CreateDirectory(FilePaths.CacheDirectory);
                Directory.CreateDirectory(FilePaths.DatabaseDirectory);
            }

            // create log file, overwriting if it's already there
            using (var logWriter = File.CreateText(FilePaths.LogPath))
            {
                logWriter.WriteLine($"ModTek v{versionString} -- {DateTime.Now}");
            }

            if (File.Exists(FilePaths.ModTekSettingsPath))
            {
                try
                {
                    SettingsDef = ModDefEx.CreateFromPath(FilePaths.ModTekSettingsPath);
                }
                catch (Exception e)
                {
                    LogException($"Error: Caught exception while parsing {FilePaths.ModTekSettingsPath}", e);
                    Finish();
                    return;
                }

                SettingsDef.Version = versionString;
            }
            else
            {
                Log("File not exists " + FilePaths.ModTekSettingsPath + " fallback to defaults");
                SettingsDef = new ModDefEx
                {
                    Enabled = true,
                    PendingEnable = true,
                    Name = MODTEK_DEF_NAME,
                    Version = versionString,
                    Description = "Mod system for HBS's PC game BattleTech.",
                    Author = "Mpstark, CptMoore, Tyler-IN, alexbartlow, janxious, m22spencer, KMiSSioN, ffaristocrat, Morphyum",
                    Website = "https://github.com/BattletechModders/ModTek"
                };
                File.WriteAllText(FilePaths.ModTekSettingsPath, JsonConvert.SerializeObject(SettingsDef, Formatting.Indented));
                SettingsDef.Directory = FilePaths.ModTekDirectory;
                SettingsDef.SaveState();
            }


            // load progress bar
            if (Enabled)
            {
                if (!ProgressPanel.Initialize(FilePaths.ModTekDirectory, $"ModTek v{versionString}"))
                {
                    Log("Error: Failed to load progress bar.  Skipping mod loading completely.");
                    Finish();
                }
            }

            // read config
            Config = Configuration.FromFile(FilePaths.ConfigPath);

            // setup assembly resolver
            TryResolveAssemblies.Add("0Harmony", Assembly.GetAssembly(typeof(HarmonyInstance)));
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var resolvingName = new AssemblyName(args.Name);
                return !TryResolveAssemblies.TryGetValue(resolvingName.Name, out var assembly) ? null : assembly;
            };

            try
            {
                var instance = HarmonyInstance.Create("io.github.mpstark.ModTek");
                instance.PatchAll(Assembly.GetExecutingAssembly());
                BattleTechResourceLoader.Refresh();
                SVGAssetLoadRequest_Load.Patch(instance);
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
            JObjectCache.Clear();
            merges = null;
            stopwatch = null;
        }

        // PATHS

        private static VersionManifestEntry FindEntry(string type, string id)
        {
            if (CustomResources.ContainsKey(type) && CustomResources[type].ContainsKey(id))
            {
                return CustomResources[type][id];
            }

            var modEntry = AddBTRLEntries.FindLast(x => x.Type == type && x.Id == id)?.GetVersionManifestEntry();
            if (modEntry != null)
            {
                return modEntry;
            }

            // if we're slating to remove an entry, then we don't want to return it here from the manifest
            return !RemoveBTRLEntries.Exists(entry => entry.Type == type && entry.Id == id)
                ? CachedVersionManifest.Find(entry => entry.Type == type && entry.Id == id)
                : null;
        }

        private static void CallFinishedLoadMethods()
        {
            Log("\nCalling FinishedLoading:");
            var assemblyMods = ModLoadOrder.Where(name => ModDefs.ContainsKey(name) && ModDefs[name].Assembly != null).ToList();
            foreach (var assemblyMod in assemblyMods)
            {
                var modDef = ModDefs[assemblyMod];
                ModDefExLoading.FinishedLoading(modDef, ModLoadOrder, CustomResources);
            }
        }

        // ADDING/REMOVING CONTENT
        private static void AddModEntry(ModEntry modEntry)
        {
            if (modEntry.Path == null)
            {
                return;
            }

            // since we're adding a new entry here, we want to remove anything that would remove it again after the fact
            if (RemoveBTRLEntries.RemoveAll(entry => entry.Id == modEntry.Id && entry.Type == modEntry.Type) > 0)
            {
                Log($"\t\t{modEntry.Id} ({modEntry.Type}) -- this entry replaced an entry that was slated to be removed. Removed the removal.");
            }

            if (CustomResources.ContainsKey(modEntry.Type))
            {
                Log($"\tAdd/Replace (CustomResource): \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type})");
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
                case nameof(SoundBankDef):
                    SoundBanks.SoundBanksFeature.AddSoundBankDef(modEntry.Path);
                    return;
                case nameof(SVGAsset):
                    Log($"Processing SVG entry of: {modEntry.Id}  type: {modEntry.Type}  name: {nameof(SVGAsset)}  path: {modEntry.Path}");
                    if (modEntry.Id.StartsWith(nameof(UILookAndColorConstants)))
                    {
                        systemIcons.Add(modEntry.Id);
                    }

                    break;
                case ModDefExLoading.CustomType_Tag:
                    CustomTags.Add(modEntry);
                    return; // Do not process further and do when the DB is updated
                case ModDefExLoading.CustomType_TagSet:
                    CustomTagSets.Add(modEntry);
                    return; // Do no process further and do when the DB is updated
            }

            // add to addendum instead of adding to manifest
            if (addendum != null)
            {
                Log($"\tAdd/Replace: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type}) [{addendum.Name}]");
            }
            else
            {
                Log($"\tAdd/Replace: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type})");
            }

            // entries in AddBTRLEntries will be added to game through patch in Patches\BattleTechResourceLocator
            AddBTRLEntries.Add(modEntry);
        }

        private static bool AddModEntryToDB(MetadataDatabase db, DBCache dbCache, string absolutePath, string typeStr)
        {
            if (Path.GetExtension(absolutePath)?.ToLowerInvariant() != ".json")
            {
                return false;
            }

            var type = (BattleTechResourceType) Enum.Parse(typeof(BattleTechResourceType), typeStr);
            var relativePath = FileUtils.GetRelativePath(absolutePath, FilePaths.GameDirectory);

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
                            if (!absolutePath.Contains(FilePaths.StreamingAssetsDirectory))
                            {
                                dbCache.Entries[relativePath] = writeTime;
                            }

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
            {
                merges[type] = new Dictionary<string, List<string>>();
            }

            if (!merges[type].ContainsKey(id))
            {
                merges[type][id] = new List<string>();
            }

            if (merges[type][id].Contains(path))
            {
                return;
            }

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
                BTRLEntriesPathes.Remove(modEntry.Path);
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
                {
                    continue;
                }

                Log($"\t\tAlso removing JSON merges for {id} ({type})");
                merges[type].Remove(id);
            }

            return removedEntry;
        }

        private static void RemoveMerge(string type, string id)
        {
            if (!merges.ContainsKey(type) || !merges[type].ContainsKey(id))
            {
                return;
            }

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

            // We intentionally DO NOT add tags and tagsets here, because AddModEntry() will skip values found in here
            //CustomResources[CustomType_Tag] = new Dictionary<string, VersionManifestEntry>();
            //CustomResources[CustomType_TagSet] = new Dictionary<string, VersionManifestEntry>();

            CustomResources.Add("DebugSettings", new Dictionary<string, VersionManifestEntry>());
            CustomResources["DebugSettings"]["settings"] = new VersionManifestEntry(
                "settings",
                Path.Combine(FilePaths.StreamingAssetsDirectory, FilePaths.DebugSettingsPath),
                "DebugSettings",
                DateTime.Now,
                "1"
            );

            CustomResources.Add("GameTip", new Dictionary<string, VersionManifestEntry>());
            CustomResources["GameTip"]["general"] = new VersionManifestEntry(
                "general",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "general.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
            CustomResources["GameTip"]["combat"] = new VersionManifestEntry(
                "combat",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "combat.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
            CustomResources["GameTip"]["lore"] = new VersionManifestEntry(
                "lore",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "lore.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );
            CustomResources["GameTip"]["sim"] = new VersionManifestEntry(
                "sim",
                Path.Combine(FilePaths.StreamingAssetsDirectory, Path.Combine("GameTips", "sim.txt")),
                "GameTip",
                DateTime.Now,
                "1"
            );

            ProgressPanel.SubmitWork(InitModsLoop);
            ProgressPanel.SubmitWork(HandleModManifestsLoop);
            ProgressPanel.SubmitWork(MergeFilesLoop);
            ProgressPanel.SubmitWork(AddToDBLoop);
            ProgressPanel.SubmitWork(SoundBanks.SoundBanksFeature.SoundBanksProcessing);
            ProgressPanel.SubmitWork(GatherDependencyTreeLoop);
            ProgressPanel.SubmitWork(FinishLoop);
        }

        private static IEnumerator<ProgressReport> GatherDependencyTreeLoop()
        {
            yield return new ProgressReport(0, "Gathering dependencies trees", "");
            if (allModDefs.Count == 0)
            {
                yield break;
            }

            var progeress = 0;
            foreach (var mod in allModDefs)
            {
                ++progeress;
                foreach (var depname in mod.Value.DependsOn)
                {
                    if (allModDefs.ContainsKey(depname))
                    {
                        if (allModDefs[depname].DependsOnMe.Contains(mod.Value) == false)
                        {
                            allModDefs[depname].DependsOnMe.Add(mod.Value);
                        }

                        ;
                    }

                    ;
                }
            }

            yield return new ProgressReport(1 / 3f, $"Gather depends on me", string.Empty, true);
            progeress = 0;
            foreach (var mod in allModDefs)
            {
                ++progeress;
                mod.Value.GatherAffectingOfflineRec();
            }

            yield return new ProgressReport(2 / 3f, $"Gather disable influence tree", string.Empty, true);
            progeress = 0;
            foreach (var mod in allModDefs)
            {
                ++progeress;
                mod.Value.GatherAffectingOnline();
            }

            yield return new ProgressReport(1, $"Gather enable influence tree", string.Empty, true);
            Log($"FAIL LIST:");
            foreach (var mod in allModDefs.Values)
            {
                if (mod.Enabled == false)
                {
                    continue;
                }

                ;
                if (mod.LoadFail == false)
                {
                    continue;
                }

                Log($"\t{mod.Name} fail {mod.FailReason}");
                foreach (var dmod in mod.AffectingOnline)
                {
                    var state = dmod.Key.Enabled && dmod.Key.LoadFail == false;
                    if (state != dmod.Value)
                    {
                        Log($"\t\tdepends on {dmod.Key.Name} should be loaded:{dmod.Value} but it is not cause enabled:{dmod.Key.Enabled} and fail:{dmod.Key.LoadFail} due to {dmod.Key.FailReason}");
                    }
                }

                foreach (var deps in mod.DependsOn)
                {
                    if (allModDefs.ContainsKey(deps) == false)
                    {
                        Log($"\t\tdepends on {deps} but abcent");
                    }
                }
            }
        }

        private static IEnumerator<ProgressReport> InitModsLoop()
        {
            yield return new ProgressReport(1, "Initializing Mods", "");

            // find all sub-directories that have a mod.json file
            var modDirectories = Directory.GetDirectories(FilePaths.ModsDirectory).Where(x => File.Exists(Path.Combine(x, FilePaths.MOD_JSON_NAME))).ToArray();
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
                var modDefPath = Path.Combine(modDirectory, FilePaths.MOD_JSON_NAME);
                try
                {
                    modDef = ModDefEx.CreateFromPath(modDefPath);
                    if (modDef.Name == MODTEK_DEF_NAME)
                    {
                        modDef = SettingsDef;
                    }
                }
                catch (Exception e)
                {
                    FailedToLoadMods.Add(FileUtils.GetRelativePath(modDirectory, FilePaths.ModsDirectory));
                    LogException($"Error: Caught exception while parsing {modDefPath}", e);
                    continue;
                }

                if (allModDefs.ContainsKey(modDef.Name) == false)
                {
                    allModDefs.Add(modDef.Name, modDef);
                }
                else
                {
                    var counter = 0;
                    var tmpname = modDef.Name;
                    do
                    {
                        ++counter;
                        tmpname = modDef.Name + "{dublicate " + counter + "}";
                    }
                    while (allModDefs.ContainsKey(tmpname) == true);

                    modDef.Name = tmpname;
                    modDef.Enabled = false;
                    modDef.LoadFail = true;
                    modDef.FailReason = "dublicate";
                    //modDef.Description = "Dublicate";
                    allModDefs.Add(modDef.Name, modDef);
                    continue;
                }

                if (!modDef.ShouldTryLoad(ModDefs.Keys.ToList(), out var reason, out var shouldAddToList))
                {
                    Log($"Not loading {modDef.Name} because {reason}");
                    if (!modDef.IgnoreLoadFailure)
                    {
                        FailedToLoadMods.Add(modDef.Name);
                        if (allModDefs.ContainsKey(modDef.Name))
                        {
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
            ModLoadOrder = LoadOrder.CreateLoadOrder(ModDefs, out var notLoaded, LoadOrder.FromFile(FilePaths.LoadOrderPath));
            foreach (var modName in notLoaded)
            {
                var modDef = ModDefs[modName];
                ModDefs.Remove(modName);
                if (modDef.IgnoreLoadFailure)
                {
                    continue;
                }

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
                        if (allModDefs.ContainsKey(modName))
                        {
                            allModDefs[modName].LoadFail = true;
                            allModDefs[modName].FailReason = $"Warning: Skipping load of {modName} because one of its dependencies failed to load.";
                        }

                        FailedToLoadMods.Add(modName);
                    }

                    continue;
                }

                yield return new ProgressReport(numModsLoaded++ / (float) ModLoadOrder.Count, "Initializing Mods", $"{modDef.Name} {modDef.Version}", true);

                try
                {
                    if (!ModDefExLoading.LoadMod(modDef, out var reason))
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
                    {
                        continue;
                    }

                    LogException($"Error: Tried to load mod: {modDef.Name}, but something went wrong. Make sure all of your JSON is correct!", e);
                    FailedToLoadMods.Add(modName);
                    if (allModDefs.ContainsKey(modName))
                    {
                        allModDefs[modName].LoadFail = true;
                        allModDefs[modName].FailReason = "Error: Tried to load mod: " + modDef.Name + ", but something went wrong. Make sure all of your JSON is correct!" + e.ToString();
                    }
                }
            }
        }

        private static IEnumerator<ProgressReport> HandleModManifestsLoop()
        {
            // there are no mods loaded, just return
            if (ModLoadOrder == null || ModLoadOrder.Count == 0)
            {
                yield break;
            }

            Log("\nAdding Mod Content...");
            var typeCache = new TypeCache(FilePaths.TypeCachePath);
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
                yield return new ProgressReport(entryCount / (float) numEntries, $"Loading {modName}", "", true);

                foreach (var modEntry in modDef.Manifest)
                {
                    yield return new ProgressReport(entryCount++ / (float) numEntries, $"Loading {modName}", modEntry.Id);

                    // type being null means we have to figure out the type from the path (StreamingAssets)
                    if (modEntry.Type == null)
                    {
                        var relativePath = FileUtils.GetRelativePath(modEntry.Path, Path.Combine(modDef.Directory, "StreamingAssets"));

                        if (relativePath == FilePaths.DebugSettingsPath)
                        {
                            modEntry.Type = "DebugSettings";
                        }
                    }

                    // type *still* being null means that this is an "non-special" case, i.e. it's in the manifest
                    if (modEntry.Type == null)
                    {
                        var relativePath = FileUtils.GetRelativePath(modEntry.Path, Path.Combine(modDef.Directory, "StreamingAssets"));
                        var fakeStreamingAssetsPath = Path.GetFullPath(Path.Combine(FilePaths.StreamingAssetsDirectory, relativePath));
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

                        // TODO fix typeCache becoming irrelevant!
                        // this is getting merged later and then added to the BTRL entries then
                        // StreamingAssets don't get default appendText
                        if (Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".json" && modEntry.ShouldMergeJSON)
                        {
                            // this assumes that vanilla .json can only have a single type
                            // typeCache will always contain this path
                            modEntry.Type = typeCache.GetTypes(modEntry.Id)[0];
                            AddMerge(modEntry.Type, modEntry.Id, modEntry.Path);
                            Log($"\tMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type})");
                            continue;
                        }

                        // TODO WTF IS THIS?? add stuff for every type? seems fishy for JSON stuff
                        // and why remove merge?
                        foreach (var type in types)
                        {
                            var subModEntry = new ModEntry(modEntry, modEntry.Path, modEntry.Id);
                            subModEntry.Type = type;
                            AddModEntry(subModEntry);
                            RemoveMerge(type, modEntry.Id);
                        }

                        continue;
                    }

                    // TODO WHY ARE TYPES SO IMPORTANT?????
                    // special handling for types
                    switch (modEntry.Type)
                    {
                        case ModDefExLoading.CustomType_AdvancedJSONMerge:
                        {
                            var advancedJSONMerge = AdvancedJSONMerge.FromFile(modEntry.Path);

                            if (!string.IsNullOrEmpty(advancedJSONMerge.TargetID) && advancedJSONMerge.TargetIDs == null)
                            {
                                advancedJSONMerge.TargetIDs = new List<string> { advancedJSONMerge.TargetID };
                            }

                            if (advancedJSONMerge.TargetIDs == null || advancedJSONMerge.TargetIDs.Count == 0)
                            {
                                Log($"\tError: AdvancedJSONMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" didn't target any IDs. Skipping this merge.");
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
                                        Log($"\tError: AdvancedJSONMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" could not resolve type for ID: {id}. Skipping this merge");
                                        continue;
                                    }

                                    // assume that only a single type
                                    type = types[0];
                                }

                                var entry = FindEntry(type, id);
                                if (entry == null)
                                {
                                    Log($"\tError: AdvancedJSONMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" could not find entry {id} ({type}). Skipping this merge");
                                    continue;
                                }

                                AddMerge(type, id, modEntry.Path);
                                Log($"\tAdvancedJSONMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" targeting '{id}' ({type})");
                            }

                            continue;
                        }
                        case nameof(SoundBankDef):
                        {
                            AddModEntry(modEntry);
                            continue;
                        }
                        case ModDefExLoading.CustomType_FixedSVGAsset:
                        {
                            AddModEntry(modEntry);
                            continue;
                        }
                        case ModDefExLoading.CustomType_Tag:
                        {
                            Log($"Processing tag of: {modEntry.Id} with type: {modEntry.Type} with path: {modEntry.Path}");
                            AddModEntry(modEntry);
                            continue;
                        }
                        case ModDefExLoading.CustomType_TagSet:
                        {
                            Log($"Processing tagset of: {modEntry.Id} with type: {modEntry.Type} with path: {modEntry.Path}");
                            AddModEntry(modEntry);
                            continue;
                        }
                    }

                    // non-StreamingAssets json merges
                    if (Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".json" && modEntry.ShouldMergeJSON ||
                        (Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".txt" || Path.GetExtension(modEntry.Path)?.ToLowerInvariant() == ".csv") && modEntry.ShouldAppendText)
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
                        Log($"\tMerge: \"{FileUtils.GetRelativePath(modEntry.Path, FilePaths.ModsDirectory)}\" ({modEntry.Type})");
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

            typeCache.ToFile(FilePaths.TypeCachePath);
            BTRLEntriesPathes = new HashSet<string>(AddBTRLEntries.Select(e => e.Path));
        }

        private static IEnumerator<ProgressReport> MergeFilesLoop()
        {
            // there are no mods loaded, just return
            if (ModLoadOrder == null || ModLoadOrder.Count == 0)
            {
                yield break;
            }

            // perform merges into cache
            Log("\nDoing merges...");
            yield return new ProgressReport(1, "Merging", "", true);

            var mergeCache = MergeCache.FromFile(FilePaths.MergeCachePath);
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
                    {
                        yield return new ProgressReport(mergeCount++ / (float) numEntries, "Merging", id);
                    }

                    var cachePath = mergeCache.GetOrCreateCachedEntry(originalPath, mergePaths);

                    // something went wrong (the parent json prob had errors)
                    if (cachePath == null)
                    {
                        continue;
                    }

                    var cacheEntry = new ModEntry(cachePath) { ShouldAppendText = false, ShouldMergeJSON = false, Type = existingEntry.Type, Id = id };

                    AddModEntry(cacheEntry);
                }
            }

            mergeCache.ToFile(FilePaths.MergeCachePath);
        }

        private static IEnumerator<ProgressReport> AddToDBLoop()
        {
            // there are no mods loaded, just return
            if (ModLoadOrder == null || ModLoadOrder.Count == 0)
            {
                yield break;
            }

            Log("\nSyncing Database...");
            yield return new ProgressReport(1, "Syncing Database", "", true);

            var dbCache = new DBCache(FilePaths.DBCachePath, FilePaths.MDDBPath, FilePaths.ModMDDBPath);
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
                var absolutePath = FileUtils.ResolvePath(path, FilePaths.GameDirectory);

                // check if the file in the db cache is still used
                if (BTRLEntriesPathes.Contains(absolutePath))
                {
                    continue;
                }

                Log($"\tNeed to remove DB entry from file in path: {path}");

                // file is missing, check if another entry exists with same filename in manifest or in BTRL entries
                var fileName = Path.GetFileName(path);
                var existingEntry = AddBTRLEntries.FindLast(x => Path.GetFileName(x.Path) == fileName)?.GetVersionManifestEntry() ?? CachedVersionManifest.Find(x => Path.GetFileName(x.FilePath) == fileName);

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
                {
                    dbCache.Entries.Remove(removeEntry);
                }

                foreach (var replacementEntry in replacementEntries)
                {
                    if (AddModEntryToDB(MetadataDatabase.Instance, dbCache, Path.GetFullPath(replacementEntry.FilePath), replacementEntry.Type))
                    {
                        Log($"\t\tReplaced DB entry with an existing entry in path: {FileUtils.GetRelativePath(replacementEntry.FilePath, FilePaths.GameDirectory)}");
                        shouldWriteDB = true;
                    }
                }
            }

            // if an entry has been removed and we cannot find a replacement, have to rebuild the mod db
            if (shouldRebuildDB)
            {
                dbCache = new DBCache(null, FilePaths.MDDBPath, FilePaths.ModMDDBPath);
            }

            Log($"\nAdding dynamic enums:");
            var addCount = 0;
            var mods = new List<ModDefEx>();
            foreach (var modname in ModLoadOrder)
            {
                if (ModDefs.ContainsKey(modname) == false)
                {
                    continue;
                }

                mods.Add(ModDefs[modname]);
            }

            foreach (var moddef in mods)
            {
                if (moddef.DataAddendumEntries.Count != 0)
                {
                    Log($"{moddef.Name}:");
                    foreach (var dataAddendumEntry in moddef.DataAddendumEntries)
                    {
                        if (AddendumUtils.LoadDataAddendum(dataAddendumEntry, moddef.Directory))
                        {
                            shouldWriteDB = true;
                        }

                        yield return new ProgressReport(addCount / (float) mods.Count, "Populating Dynamic Enums", moddef.Name);
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
                    yield return new ProgressReport(addCount / (float) AddBTRLEntries.Count, "Populating Database", modEntry.Id);
                    Log($"\tAdded/Updated {modEntry.Id} ({modEntry.Type})");
                    shouldWriteDB = true;
                }

                addCount++;
            }

            // Add any custom tags to DB
            if (CustomTags.Count > 0)
            {
                Log($"Processing CustomTags:");
            }

            foreach (var modEntry in CustomTags)
            {
                CustomTypeProcessor.AddOrUpdateTag(modEntry.Path);
            }

            if (CustomTagSets.Count > 0)
            {
                Log($"Processing CustomTagSets:");
            }

            foreach (var modEntry in CustomTagSets)
            {
                CustomTypeProcessor.AddOrUpdateTagSet(modEntry.Path);
            }

            //ModLoadOrder.Count;

            dbCache.ToFile(FilePaths.DBCachePath);

            if (shouldWriteDB)
            {
                yield return new ProgressReport(1, "Writing Database", "", true);
                Log("Writing DB");

                var inMemoryDB = typeof(MetadataDatabase).GetField("inMemoryDB", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(MetadataDatabase.Instance);
                Task.Run(
                    () =>
                    {
                        lock (inMemoryDB)
                        {
                            MetadataDatabase.Instance.WriteInMemoryDBToDisk();
                        }
                    }
                );
            }
        }

        private static IEnumerator<ProgressReport> FinishLoop()
        {
            // "Loop"
            yield return new ProgressReport(1, "Finishing Up", "", true);
            Log("\nFinishing Up");

            if (CustomResources["DebugSettings"]["settings"].FilePath != Path.Combine(FilePaths.StreamingAssetsDirectory, FilePaths.DebugSettingsPath))
            {
                DebugBridge.LoadSettings(CustomResources["DebugSettings"]["settings"].FilePath);
            }

            if (ModLoadOrder != null && ModLoadOrder.Count > 0)
            {
                CallFinishedLoadMethods();
                HarmonyUtils.PrintHarmonySummary(FilePaths.HarmonySummaryPath);
                LoadOrder.ToFile(ModLoadOrder, FilePaths.LoadOrderPath);
            }

            Config?.ToFile(FilePaths.ConfigPath);

            Finish();
        }
    }
}
