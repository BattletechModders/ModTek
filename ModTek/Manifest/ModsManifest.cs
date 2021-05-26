using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using Harmony;
using ModTek.Logging;
using ModTek.Manifest.MDD;
using ModTek.Manifest.Merges;
using ModTek.Manifest.Mods;
using ModTek.Misc;
using ModTek.SoundBanks;
using ModTek.UI;
using ModTek.Util;
using SVGImporter;

namespace ModTek.Manifest
{
    internal static class ModsManifest
    {
        private static MergesDatabase mergesDatabase = new();

        private static HashSet<string> systemIcons = new();
        internal static HashSet<ModEntry> CustomTags = new();
        internal static HashSet<ModEntry> CustomTagSets = new();

        private static List<ModEntry> AddBTRLEntries = new();

        private static HashSet<string> AddBTRLEntryPaths;

        internal static bool IsBTRLEntryCached(string absolutePath)
        {
            return AddBTRLEntryPaths.Contains(absolutePath);
        }

        internal static Dictionary<string, Dictionary<string, VersionManifestEntry>> CustomResources = new();

        internal static bool isInSystemIcons(string id)
        {
            return systemIcons.Contains(id);
        }

        internal static void PrepareManifestAndCustomResources()
        {
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
        }

        internal static IEnumerator<ProgressReport> ProcessLoop()
        {
            // there are no mods loaded, just return
            if (ModDefsDatabase.ModLoadOrder == null || ModDefsDatabase.ModLoadOrder.Count == 0)
            {
                return Enumerable.Empty<ProgressReport>().GetEnumerator();
            }

            return CSharpUtils.Enumerate(
                HandleModManifestsLoop(),
                MDDHelper.AddToDBLoop()
            );
        }

        private static IEnumerator<ProgressReport> HandleModManifestsLoop()
        {
            Logger.Log("\nAdding Mod Content...");

            // progress panel setup
            var entryCount = 0;
            var numEntries = 0;
            ModDefsDatabase.ModDefs.Do(entries => numEntries += entries.Value.Manifest.Count);

            foreach (var modDef in ModDefsDatabase.ModsWithManifests())
            {
                var modName = modDef.Name;

                Logger.Log($"{modName}:");
                yield return new ProgressReport(entryCount / (float) numEntries, $"Loading {modName}", "", true);

                foreach (var modEntry in modDef.Manifest)
                {
                    if (modEntry.Path == null)
                    {
                        Logger.Log($"\tWarning: Internal error, path not set for {modName} {modEntry.Id}. NOT LOADING THIS FILE");
                        continue;
                    }

                    yield return new ProgressReport(entryCount++ / (float) numEntries, $"Loading {modName}", modEntry.Id);

                    NormalizeModEntries(modEntry);
                }
            }

            AddBTRLEntryPaths = new HashSet<string>(AddBTRLEntries.Select(e => e.Path));
        }

        private static void NormalizeModEntries(ModEntry entry)
        {
            if (entry.IsFile)
            {
                if (string.IsNullOrEmpty(entry.Id))
                {
                    entry.Id = entry.FileNameWithoutExtension;
                }
                if (entry.IsTypeStreamingAsset)
                {
                    if (entry.Id == "settings")
                    {
                        entry.Type = "DebugSettings";
                    }
                }
                AddModEntry(entry);
            }
            else if (entry.IsDirectory)
            {
                foreach (var file in entry.Files)
                {
                    var id = Path.GetFileNameWithoutExtension(file);
                    AddModEntry(new ModEntry(entry, file, id));
                }
            }
            else
            {
                Logger.Log($"\tWarning: Could not find path {entry.RelativePathToMods}.");
            }
        }

        private static void AddModEntry(ModEntry entry)
        {
            if (entry.ShouldMergeJSON || entry.ShouldAppendText)
            {
                if ((entry.ShouldMergeJSON && entry.IsJson)
                    || entry.ShouldAppendText && (entry.IsTxt || entry.IsCsv))
                {
                    mergesDatabase.AddModEntry(entry);
                }
                else
                {
                    Logger.Log($"\tError: ShouldMergeJSON requires .json and ShouldAppendText requires .txt or .csv: \"{entry.RelativePathToMods}\".");
                }
            }
            else if (entry.IsTypeCustomResource)
            {
                Logger.Log($"\tAdd/Replace (CustomResource): \"{FileUtils.GetRelativePath(FilePaths.ModsDirectory, entry.Path)}\" ({entry.Type})");
                CustomResources[entry.Type][entry.Id] = entry.GetVersionManifestEntry();
            }
            else if (entry.IsTypeSoundBankDef)
            {
                SoundBanksFeature.AddSoundBankDef(entry.Path);
            }
            else if (entry.IsTypeCustomTag)
            {
                CustomTags.Add(entry);
            }
            else if (entry.IsTypeCustomTagSet)
            {
                CustomTagSets.Add(entry);
            }
            else if (entry.IsTypeBattleTechResourceType)
            {
                var resourceType = entry.ResourceType;
                if (resourceType is BattleTechResourceType.SVGAsset)
                {
                    Logger.Log($"Processing SVG entry of: {entry.Id}  type: {entry.Type}  name: {nameof(SVGAsset)}  path: {entry.Path}");
                    if (entry.Id.StartsWith(nameof(UILookAndColorConstants)))
                    {
                        systemIcons.Add(entry.Id);
                    }
                }

                Logger.Log($"\tAdd/Replace: \"{FileUtils.GetRelativePath(FilePaths.ModsDirectory, entry.Path)}\" ({entry.Type})");

                // entries in AddBTRLEntries will be added to game through patch in Patches\BattleTechResourceLocator
                AddBTRLEntries.Add(entry); // TODO new resource adding here
            }
            else
            {
                Logger.Log($"\tError: Type of entry unknown: \"{entry.RelativePathToMods}\".");
            }
        }

        internal static void FinalizeResourceLoading()
        {
            if (CustomResources["DebugSettings"]["settings"].FilePath != Path.Combine(FilePaths.StreamingAssetsDirectory, FilePaths.DebugSettingsPath))
            {
                DebugBridge.LoadSettings(CustomResources["DebugSettings"]["settings"].FilePath);
            }
        }

        internal static VersionManifestEntry FindEntryByFileName(string fileName)
        {
            return AddBTRLEntries.FindLast(x => Path.GetFileName(x.Path) == fileName)?.GetVersionManifestEntry()
                ?? ModDefsDatabase.CachedVersionManifest.Find(x => Path.GetFileName(x.FilePath) == fileName);
        }

        internal static List<ModEntry> GetAddToDbEntries()
        {
            return AddBTRLEntries.Where(x => x.AddToDB).ToList();
        }

        internal static string GetMergedContent(string bundleName, string id, DateTime version)
        {
            return mergesDatabase.GetMergedContent(bundleName, id, version);
        }

        internal static string MergeOriginalContent(string bundleName, string id, DateTime version, string originalContent)
        {
            return mergesDatabase.MergeContentIfApplicable(bundleName, id, version, originalContent);
        }
    }
}
