using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using BattleTech;
using BattleTechModLoader;
using Harmony;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine.Assertions;

namespace ModTek
{
    using static Logger;

    [UsedImplicitly]
    public static class ModTek
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public static string ModDirectory { get; private set; }

        // ReSharper disable once InconsistentNaming
        private const string MOD_JSON_NAME = "mod.json";

        internal static Dictionary<string, ModDef.ManifestEntry> NewManifestEntries { get; } =
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
            var modDefs = new List<ModDef>();
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

                    modDefs.Add(modDef);
                }
                catch (Exception e)
                {
                    Log($"Caught exception while parsing {MOD_JSON_NAME} at path {modDefPath}");
                    Log($"Exception: {e}");
                    //continue;
                }
            }

            // TODO: be able to read load order from a JSON
            var loadOrder = GetLoadOrder(modDefs, out var willNotLoad);
            while (loadOrder.Count > 0)
            {
                var modDef = loadOrder.Dequeue();
                LoadMod(modDef);
            }

            foreach (var modDef in willNotLoad)
            {
                LogWithDate("Will not load {0} because its dependancies are unmet.", modDef.Name);
            }
        }

        [SuppressMessage("ReSharper", "ParameterTypeCanBeEnumerable.Global")]
        // ReSharper disable once ParameterTypeCanBeEnumerable.Local
        // ReSharper disable once MemberCanBePrivate.Global
        internal static Queue<ModDef> GetLoadOrder(IList<ModDef> modDefs, out List<ModDef> unloaded)
        {
            var loadOrder = new Queue<ModDef>();
            var loaded = new HashSet<string>();
            unloaded = modDefs.OrderByDescending(x => x.Name).ToList();

            int removedThisPass;
            do
            {
                removedThisPass = 0;

                for (var i = unloaded.Count - 1; i >= 0; i--)
                {
                    var modDef = unloaded[i];
                    if (modDef.DependsOn != null && modDef.DependsOn.Count != 0 &&
                        modDef.DependsOn.Intersect(loaded).Count() != modDef.DependsOn.Count) continue;
                    unloaded.RemoveAt(i);
                    loadOrder.Enqueue(modDef);
                    loaded.Add(modDef.Name);
                    removedThisPass++;
                }
            } while (removedThisPass > 0 && unloaded.Count > 0);

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

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once InconsistentNaming
        public static string InferIDFromFileAndType(string path, string type)
        {
            var ext = Path.GetExtension(path);

            Assert.IsNotNull(ext, nameof(ext) + " != null");
            if (ext.ToLower() != "json" || !File.Exists(path)) return Path.GetFileNameWithoutExtension(path);
            try
            {
                var jObj = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(path));

                // go through the different kinds of id storage in JSONS
                // TODO: make this specific to the type
                string[] jPaths = {"Description.Id", "id", "Id", "ID", "identifier", "Identifier"};
                foreach (var jPath in jPaths)
                {
                    var id = (string) jObj.SelectToken(jPath);
                    if (id != null)
                        return id;
                }
            }
            catch (Exception e)
            {
                Log($"\tException occurred while parsing type {type} json at {path}: {e}");
            }

            // fall back to using the path
            return Path.GetFileNameWithoutExtension(path);
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
                        LogWithDate(
                            $"{modDef.Name} has a manifest entry that is missing its type or path! Aborting load.");
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
                    new object[] {modDef.Directory, modDef.Settings.ToString(Formatting.None)});
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

            LogWithDate("Loaded {0}", modDef.Name);
        }
    }
}