using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using HBS;
using ModTek.Features.AdvJSONMerge;
using ModTek.Features.CustomStreamingAssets;
using ModTek.Features.CustomSVGAssets;
using ModTek.Features.CustomTags;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;
using ModTek.Features.Manifest.Merges;
using ModTek.Features.Manifest.Mods;
using ModTek.Features.SoundBanks;
using ModTek.Misc;
using ModTek.UI;
using ModTek.Util;
using static ModTek.Features.Logging.MTLogger;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ModTek.Features.Manifest
{
    internal static class ModsManifest
    {
        private static readonly MergeCache mergeCache = new MergeCache();
        private static readonly MDDBCache mddbCache = new MDDBCache();
        internal static readonly MTContentPackManager bundleManager = new MTContentPackManager();

        internal static IEnumerator<ProgressReport> HandleModManifestsLoop()
        {
            var sw = new Stopwatch();
            sw.Restart();
            bundleManager.LoadAllContentPacks();
            LogIfSlow(sw, "LoadAllContentPacks");

            BetterBTRL.Instance.RefreshTypedEntries();

            sw.Restart();
            foreach (var p in BuildModdedBTRL())
            {
                yield return p;
            }
            LogIfSlow(sw, "Build Modded Resource Locator");

            BetterBTRL.Instance.RefreshTypedEntries();

            sw.Restart();
            foreach (var p in mergeCache.BuildCache())
            {
                yield return p;
            }
            LogIfSlow(sw, "Merge Cache Build and Cleanup");

            BetterBTRL.Instance.RefreshTypedEntries();

            sw.Restart();
            foreach (var p in mddbCache.BuildCache())
            {
                yield return p;
            }
            LogIfSlow(sw, "MDDB Cache Build and Cleanup");

            bundleManager.UnloadAll();

            BetterBTRL.Instance.RefreshTypedEntries();
        }

        private static IEnumerable<ProgressReport> BuildModdedBTRL()
        {
            var sliderText = "Processing Manifests";
            yield return new ProgressReport(0, sliderText, "", true);

            var mods = ModDefsDatabase.ModsInLoadOrder();
            LogIf(mods.Count > 0, "Processing Mod Manifests...");

            var countCurrent = 0;
            var countMax = (float) mods.Count;

            foreach (var modDef in mods)
            {
                yield return new ProgressReport(countCurrent++/countMax, sliderText, modDef.Name, true);

                AddImplicitManifest(modDef);

                LogIf(modDef.Manifest.Count > 0, $"{modDef.QuotedName} Manifest:");
                var packager = new ModAddendumPackager(modDef.Name);
                foreach (var modEntry in modDef.Manifest)
                {
                    NormalizeAndExpandAndAddModEntries(modDef, modEntry, packager);
                }

                packager.SaveToBTRL();
            }
        }

        private static void AddImplicitManifest(ModDefEx modDef)
        {
            if (!modDef.LoadImplicitManifest)
            {
                return;
            }

            if (Directory.Exists(modDef.GetFullPath(FilePaths.StreamingAssetsDirectoryName)))
            {
                modDef.Manifest.Add(new ModEntry
                {
                    Path = FilePaths.StreamingAssetsDirectoryName,
                    ShouldMergeJSON = ModTek.Config.ImplicitManifestShouldMergeJSON,
                    ShouldAppendText = ModTek.Config.ImplicitManifestShouldAppendText
                });
            }

            if (Directory.Exists(modDef.GetFullPath(FilePaths.ModdedContentPackDirectoryName)))
            {
                modDef.Manifest.Add(new ModEntry
                {
                    Path = FilePaths.ModdedContentPackDirectoryName,
                    ShouldMergeJSON = ModTek.Config.ImplicitManifestShouldMergeJSON,
                    ShouldAppendText = ModTek.Config.ImplicitManifestShouldAppendText
                });
            }
        }

        private static void NormalizeAndExpandAndAddModEntries(ModDefEx modDef, ModEntry entry, ModAddendumPackager packager)
        {
            entry.ModDef = modDef;

            if (entry.AssetBundleName != null)
            {
                AddModEntry(entry, packager);
            }
            else if (entry.IsFile)
            {
                if (BTConstants.CType(entry.Type, out var customType) && customType == CustomType.AdvancedJSONMerge)
                {
                    ExpandAdvancedMerges(entry, packager);
                }
                else
                {
                    AddModEntry(entry, packager);
                }

            }
            else if (entry.IsDirectory)
            {
                if (entry.IsModdedContentPackBasePath)
                {
                    ExpandModdedContentPack(modDef, entry, packager);
                }
                else
                {
                    var patterns = entry.Type == nameof(SoundBankDef) ? new []{FileUtils.JSON_TYPE} : null;
                    foreach (var file in FileUtils.FindFiles(entry.AbsolutePath, patterns))
                    {
                        var copy = entry.copy();
                        copy.Path = FileUtils.GetRelativePath(modDef.Directory, file);
                        NormalizeAndExpandAndAddModEntries(modDef, copy, packager); // could lead to adv json merges that again expand
                    }
                }
            }
            else
            {
                Log($"\tWarning: Could not find path {entry.RelativePathToMods}.");
            }
        }

        private static void ExpandAdvancedMerges(ModEntry entry, ModAddendumPackager packager)
        {
            var advMerge = AdvancedJSONMerge.FromFile(entry.AbsolutePath);
            if (advMerge == null)
            {
                return;
            }

            var targets = new List<string>();
            if (!string.IsNullOrEmpty(advMerge.TargetID))
            {
                targets.Add(advMerge.TargetID);
            }

            if (advMerge.TargetIDs != null)
            {
                targets.AddRange(advMerge.TargetIDs);
            }

            if (targets.Count == 0)
            {
                targets.Add(entry.FileNameWithoutExtension);
            }

            foreach (var target in targets)
            {
                var copy = entry.copy();
                copy.Id = target;
                copy.Type = advMerge.TargetType;
                copy.ShouldMergeJSON = true;
                AddModEntry(copy, packager);
            }
        }

        private static void ExpandModdedContentPack(ModDefEx modDef, ModEntry entry, ModAddendumPackager packager)
        {
            foreach (var packPath in Directory.GetDirectories(entry.AbsolutePath))
            {
                var contentPackName = Path.GetFileName(packPath);
                if (!bundleManager.HasContentPack(contentPackName))
                {
                    Log($"Unknown content pack {contentPackName} in {entry.AbsolutePath}");
                    continue;
                }

                foreach (var typesPath in Directory.GetDirectories(packPath))
                {
                    var typeName = Path.GetFileName(typesPath);
                    if (!BTConstants.BTResourceType(typeName, out _))
                    {
                        Log($"Unknown resource type {typeName} in {packPath}");
                        continue;
                    }

                    foreach (var file in FileUtils.FindFiles(typesPath))
                    {
                        var copy = entry.copy();
                        copy.Path = FileUtils.GetRelativePath(modDef.Directory, file);
                        copy.Type = typeName;
                        copy.RequiredContentPacks = new[]
                        {
                            contentPackName
                        };
                        AddModEntry(copy, packager);
                    }
                }
            }
        }

        private static void AddModEntry(ModEntry entry, ModAddendumPackager packager)
        {
            if (!FixMissingIdAndType(entry))
            {
                return;
            }

            if (AddModEntryAsMerge(entry))
            {
                return;
            }

            if (AddModEntryAsBTR(entry, packager))
            {
                return;
            }

            if (entry.IsTypeCustomStreamingAsset)
            {
                LogModEntryAction("Replace", entry);
                packager.AddEntry(entry);
                return;
            }

            if (AddModEntryAsCR(entry, packager))
            {
                return;
            }

            if (SoundBanksFeature.Add(entry))
            {
                LogModEntryAction("Set", entry);
                return;
            }

            if (CustomTagFeature.Add(entry))
            {
                LogModEntryAction("Set", entry);
                return;
            }

            Log($"\tError: Type of entry unknown: {entry}.");
        }

        private static bool FixMissingIdAndType(ModEntry entry)
        {
            // fix missing id
            if (string.IsNullOrEmpty(entry.Id))
            {
                entry.Id = entry.FileNameWithoutExtension;
            }

            if (!string.IsNullOrEmpty(entry.Type))
            {
                return true;
            }

            if (CustomStreamingAssetsFeature.FindAndSetMatchingCustomStreamingAssetsType(entry))
            {
                return true;
            }

            var ext = entry.Path.GetExtension();
            var entriesById = BetterBTRL.Instance.EntriesByID(entry.Id)
                .Where(x => ext.Equals(x.GetRawPath().GetExtension()))
                .ToList();

            if (entriesById.Count == 0)
            {
                Log($"\t\tError: Can't resolve type, no types found for id and extension, either an issue with mod order or typo (case sensitivity): {entry}");
                return false;
            }
            if (entriesById.Count > 1)
            {
                Log($"\t\tError: Can't resolve type, more than one type found for id and extension, please specify manually: {entry}");
                return false;
            }
            entry.Type = entriesById[0].Type;
            return true;
        }

        private static bool AddModEntryAsMerge(ModEntry entry)
        {
            if (entry.ShouldMergeJSON && entry.IsJson)
            {
                LogModEntryAction("Merge", entry);
                mergeCache.AddModEntry(entry);
                return true;
            }

            if (entry.ShouldAppendText && (entry.IsTxt || entry.IsCsv))
            {
                LogModEntryAction("Append", entry);
                mergeCache.AddModEntry(entry);
                return true;
            }

            return false;
        }

        private static bool AddModEntryAsBTR(ModEntry entry, ModAddendumPackager packager)
        {
            if (!BTConstants.BTResourceType(entry.Type, out var resourceType))
            {
                return false;
            }

            if (resourceType is BattleTechResourceType.SVGAsset)
            {
                LogModEntryAction("SVGAsset", entry);
                SVGAssetFeature.OnAddSVGEntry(entry);
            }

            if (entry.AddToAddendum != null)
            {
                LogModEntryAction("AddToAddendum", entry);
                BetterBTRL.Instance.AddAddendumOverrideEntry(entry.AddToAddendum, entry.CreateVersionManifestEntry());
            }
            else
            {
                if (BetterBTRL.Instance.EntryByIDAndType(entry.Id, entry.Type) != null)
                {
                    LogModEntryAction("Replace", entry);
                    if (!entry.AddToDB)
                    {
                        Log($"\t\tAddToDB=false ignored due to replacement");
                    }
                }
                else
                {
                    LogModEntryAction("Add", entry);
                    if (!entry.AddToDB)
                    {
                        mddbCache.AddToNotIndexable(entry);
                        Log($"\t\tAddToDB=false");
                    }
                }

                packager.AddEntry(entry);
            }

            return true;
        }

        private static bool AddModEntryAsCR(ModEntry entry, ModAddendumPackager packager)
        {
            if (!entry.IsTypeCustomResource)
            {
                return false;
            }

            if (entry.RequiredContentPacks != null && entry.RequiredContentPacks.Length > 0)
            {
                // TODO check if hooking into ownership check works with custom resources (probably yes if type not relevant)!
                Log($"\tError: Custom resources don't support RequiredContentPacks. {entry}");
                return true;
            }

            if (BetterBTRL.Instance.EntryByIDAndType(entry.Id, entry.Type) != null)
            {
                LogModEntryAction("Replace", entry);
                if (!entry.AddToDB)
                {
                    Log($"\t\tAddToDB=false ignored due to replacement");
                }
            }
            else
            {
                LogModEntryAction("Add", entry);
                if (!entry.AddToDB)
                {
                    Log($"\t\tAddToDB=false");
                    mddbCache.AddToNotIndexable(entry);
                }
            }

            packager.AddEntry(entry);
            return true;
        }

        private static void LogModEntryAction(string action, ModEntry entry)
        {
            Log($"\t{action}: {entry}");
        }

        internal static string GetJson(VersionManifestEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.AssetBundleName))
            {
                return bundleManager.GetText(entry.AssetBundleName, entry.Id);
            }

            try
            {
                return File.ReadAllText(entry.FilePath);
            }
            catch (Exception e)
            {
                Log($"\t\tError: Can't read: {entry}", e);
                return null;
            }
        }

        internal static void ContentPackManifestsLoaded()
        {
            // required to make sure IntroCinematicLauncher is initialized
            // so the HoldForIntroVideo + OnCinematicEnd pattern can be used later
            if (LazySingletonBehavior<UIManager>.Instance.GetFirstModule<MainMenu>() == null)
            {
                Log("MainMenu module not yet loaded, delaying VerifyCaches");
                UnityGameInstance.BattleTechGame.MessageCenter.AddFiniteSubscriber(
                    MessageCenterMessageType.OnEnterMainMenu,
                    _ =>
                    {
                        ContentPackManifestsLoaded();
                        return true;
                    }
                );
                return;
            }

            // if cinematic launcher is playing or wants to play video, let's wait
            if (IntroCinematicLauncher.HoldForIntroVideo)
            {
                Log("HoldForIntroVideo, delaying VerifyCaches");
                UnityGameInstance.BattleTechGame.MessageCenter.AddFiniteSubscriber(
                    MessageCenterMessageType.OnCinematicEnd,
                    _ =>
                    {
                        ContentPackManifestsLoaded();
                        return true;
                    }
                );
                return;
            }

            ShowModsFailedPopupOrContinue();
        }

        private static void ShowModsFailedPopupOrContinue()
        {
            if (ModDefsDatabase.FailedToLoadMods.Count == 0)
            {
                ModsManifestPreloader.PrewarmResourcesIfEnabled();
                return;
            }

            GenericPopupBuilder.Create(
                    "Some Mods Didn't Load",
                    "Continuing might break your game." +
                    $"\nCheck \"{FilePaths.LogPathRelativeToGameDirectory}\" for more info" +
                    "\n\n" + string.Join(", ", ModDefsDatabase.FailedToLoadMods.ToArray())
                )
                .AddButton("Risk Continuing", ModsManifestPreloader.PrewarmResourcesIfEnabled)
                .AddButton("Quit Game", UnityGameInstance.Instance.ShutdownGame, false)
                .Render();
            ModDefsDatabase.FailedToLoadMods.Clear();
        }
    }
}
