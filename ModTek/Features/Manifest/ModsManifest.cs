using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using HBS;
using ModTek.Features.AdvJSONMerge;
using ModTek.Features.CustomDebugSettings;
using ModTek.Features.CustomGameTips;
using ModTek.Features.CustomSVGAssets;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;
using ModTek.Features.Manifest.Merges;
using ModTek.Features.Manifest.Mods;
using ModTek.Misc;
using ModTek.UI;
using ModTek.Util;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ModTek.Features.Manifest;

internal static class ModsManifest
{
    private static readonly MergeCache mergeCache = new();
    private static readonly MDDBCache mddbCache = new();
    internal static readonly MTContentPackManager bundleManager = new();

    internal static IEnumerator<ProgressReport> HandleModManifestsLoop()
    {
        var sw = new Stopwatch();
        sw.Restart();
        bundleManager.LoadAllContentPacks();
        Log.Main.Debug?.LogIfSlow(sw, "LoadAllContentPacks");
        // lets assume we own everything during merging and indexing
        BetterBTRL.Instance.PackIndex.AllContentPacksOwned = true;

        BetterBTRL.Instance.RefreshTypedEntries();

        sw.Restart();
        foreach (var p in BuildModdedBTRL())
        {
            yield return p;
        }
        Log.Main.Debug?.LogIfSlow(sw, "BuildModdedBTRL");

        BetterBTRL.Instance.RefreshTypedEntries();

        sw.Restart();
        foreach (var p in mergeCache.BuildCache())
        {
            yield return p;
        }
        Log.Main.Debug?.LogIfSlow(sw, "BuildMergeCache");

        BetterBTRL.Instance.RefreshTypedEntries();

        sw.Restart();
        foreach (var p in mddbCache.BuildCache())
        {
            yield return p;
        }
        Log.Main.Debug?.LogIfSlow(sw, "BuildMDDBCache");

        BetterBTRL.Instance.PackIndex.AllContentPacksOwned = false;
        bundleManager.UnloadAll();

        BetterBTRL.Instance.RefreshTypedEntries();
    }

    private static IEnumerable<ProgressReport> BuildModdedBTRL()
    {
        var sliderText = "Processing Manifests";
        yield return new ProgressReport(0, sliderText, "", true);

        var mods = ModDefsDatabase.ModsInLoadOrder();
        Log.Main.Info?.LogIf(mods.Count > 0, "Processing Mod Manifests...");

        var countCurrent = 0;
        var countMax = (float) mods.Count;

        foreach (var modDef in mods)
        {
            yield return new ProgressReport(countCurrent++/countMax, sliderText, modDef.Name, true);

            AddImplicitManifest(modDef);

            Log.Main.Info?.LogIf(modDef.Manifest.Count > 0, $"{modDef.QuotedName} Manifest:");
            foreach (var modEntry in modDef.Manifest)
            {
                NormalizeAndExpandAndAddModEntries(modDef, modEntry);
            }
        }
    }

    private static void AddImplicitManifest(ModDefEx modDef)
    {
        if (!modDef.LoadImplicitManifest)
        {
            return;
        }

        const string streamingAssetsDirectoryName = "StreamingAssets";
        if (Directory.Exists(modDef.GetFullPath(streamingAssetsDirectoryName)))
        {
            modDef.Manifest.Add(new ModEntry
            {
                Path = streamingAssetsDirectoryName,
                ShouldMergeJSON = ModTek.Config.ImplicitManifestShouldMergeJSON,
                ShouldAppendText = ModTek.Config.ImplicitManifestShouldAppendText
            });
        }
    }

    private static void NormalizeAndExpandAndAddModEntries(ModDefEx modDef, ModEntry entry)
    {
        entry.ModDef = modDef;

        if (entry.AssetBundleName != null)
        {
            AddModEntry(entry);
        }
        else if (entry.IsFile)
        {
            if (BTConstants.CType(entry.Type, out var customType) && customType == CustomType.AdvancedJSONMerge)
            {
                ExpandAdvancedMerges(entry);
            }
            else
            {
                AddModEntry(entry);
            }

        }
        else if (entry.IsDirectory)
        {
            var patterns = entry.Type == nameof(SoundBankDef) ? new []{FileUtils.JSON_TYPE} : null;
            foreach (var file in FileUtils.FindFiles(entry.AbsolutePath, patterns))
            {
                var copy = entry.copy();
                copy.Path = FileUtils.GetRealRelativePath(file, modDef.Directory);
                NormalizeAndExpandAndAddModEntries(modDef, copy); // could lead to adv json merges that again expand
            }
        }
        else
        {
            Log.Main.Warning?.Log($"\tCould not find path {entry.RelativePathToMods}.");
        }
    }

    private static void ExpandAdvancedMerges(ModEntry entry)
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
            AddModEntry(copy);
        }
    }

    private static void AddModEntry(ModEntry entry)
    {
        if (!FixMissingIdAndType(entry))
        {
            return;
        }

        if (AddModEntryAsMerge(entry))
        {
            return;
        }

        if (AddModEntryAsBTR(entry))
        {
            return;
        }

        if (AddModEntryAsCR(entry))
        {
            return;
        }

        Log.Main.Warning?.Log($"\tType of entry unknown: {entry}.");
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

        if (DebugSettingsFeature.FindAndSetMatchingType(entry))
        {
            return true;
        }
        if (GameTipsFeature.FindAndSetMatchingType(entry))
        {
            return true;
        }

        var ext = entry.Path.GetExtension() ?? "";
        var entriesById = BetterBTRL.Instance.EntriesByID(entry.Id)
            .Where(x => ext.Equals(x.GetRawPath().GetExtension() ?? ""))
            .ToList();

        if (entriesById.Count == 0)
        {
            Log.Main.Warning?.Log($"\t\tCan't resolve type, no types found for id and extension, either an issue with mod order or typo (case sensitivity): {entry}");
            return false;
        }
        if (entriesById.Count > 1)
        {
            Log.Main.Warning?.Log($"\t\tCan't resolve type, more than one type found for id and extension, please specify manually: {entry}");
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
            UnsupportedFeatureAddToDb(entry);
            UnsupportedFeatureContentPackRequirements(entry);
            mergeCache.AddModEntry(entry);
            return true;
        }

        if (entry.ShouldAppendText && (entry.IsTxt || entry.IsCsv))
        {
            LogModEntryAction("Append", entry);
            UnsupportedFeatureAddToDb(entry);
            UnsupportedFeatureContentPackRequirements(entry);
            mergeCache.AddModEntry(entry);
            return true;
        }

        return false;
    }

    private static bool AddModEntryAsBTR(ModEntry entry)
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
                UnsupportedFeatureAddToDb(entry);
                UnsupportedFeatureContentPackRequirements(entry);
                AddEntryToBTRL(entry);
            }
            else
            {
                LogModEntryAction("Add", entry);
                AddToDbIfApplicable(entry);
                AddEntryToBTRL(entry);
            }
        }

        return true;
    }

    private static bool AddModEntryAsCR(ModEntry entry)
    {
        if (!entry.IsTypeCustomResource)
        {
            return false;
        }

        if (BetterBTRL.Instance.EntryByIDAndType(entry.Id, entry.Type) != null)
        {
            LogModEntryAction("Replace", entry);
            UnsupportedFeatureAddToDb(entry);
            UnsupportedFeatureContentPackRequirements(entry);
            AddEntryToBTRL(entry);
        }
        else
        {
            LogModEntryAction("Add", entry);
            AddToDbIfApplicable(entry);
            AddEntryToBTRL(entry);
        }
        return true;
    }

    private static void AddEntryToBTRL(ModEntry entry)
    {
        TrackModEntryOwnershipIfApplicable(entry);
        mergeCache.ClearQueuedMergesForEntryIfApplicable(entry);
        BetterBTRL.Instance.AddModEntry(entry);
    }

    private static void TrackModEntryOwnershipIfApplicable(ModEntry entry)
    {
        if (entry.RequiredContentPack == null)
        {
            return;
        }

        // type independent check
        var types = BetterBTRL.Instance.EntriesByID(entry.Id);
        if (types.Length > 0)
        {
            Log.Main.Warning?.Log($"Detected existing entry with same resource id ({entry.Id}), ignoring specified {nameof(ModEntry.RequiredContentPack)}.");
            return;
        }

        BetterBTRL.Instance.PackIndex.TrackModEntry(entry);
    }

    private static void LogModEntryAction(string action, ModEntry entry)
    {
        Log.Main.Info?.Log($"\t{action}: {entry}");
    }

    private static void AddToDbIfApplicable(ModEntry entry)
    {
        if (!entry.AddToDB)
        {
            mddbCache.AddToNotIndexable(entry);
            Log.Main.Info?.Log($"\t\tNot indexing to MDDB.");
        }
    }

    private static void UnsupportedFeatureContentPackRequirements(ModEntry entry)
    {
        if (entry.RequiredContentPack != null)
        {
            Log.Main.Warning?.Log($"\t\tSpecified {nameof(entry.RequiredContentPack)} is being ignored.");
            entry.RequiredContentPack = null;
        }
    }

    private static void UnsupportedFeatureAddToDb(ModEntry entry)
    {
        if (!entry.AddToDB)
        {
            Log.Main.Warning?.Log($"\t\t{nameof(entry.AddToDB)}={entry.AddToDB} is being ignored");
        }
    }

    internal static string GetText(VersionManifestEntry entry)
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
            Log.Main.Warning?.Log($"\t\tCan't read file at path {entry.FilePath}", e);
            return null;
        }
    }

    internal static void ContentPackManifestsLoaded()
    {
        // required to make sure IntroCinematicLauncher is initialized
        // so the HoldForIntroVideo + OnCinematicEnd pattern can be used later
        if (LazySingletonBehavior<UIManager>.Instance.GetFirstModule<MainMenu>() == null)
        {
            Log.Main.Info?.Log("MainMenu module not yet loaded, delaying VerifyCaches");
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
            Log.Main.Info?.Log("HoldForIntroVideo, delaying VerifyCaches");
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
                $"\nCheck \"{FileUtils.GetRelativePath(FilePaths.LogPath)}\" for more info" +
                "\n\n" + string.Join(", ", ModDefsDatabase.FailedToLoadMods.ToArray())
            )
            .AddButton("Risk Continuing", ModsManifestPreloader.PrewarmResourcesIfEnabled)
            .AddButton("Quit Game", UnityGameInstance.Instance.ShutdownGame, false)
            .Render();
        ModDefsDatabase.FailedToLoadMods.Clear();
    }
}