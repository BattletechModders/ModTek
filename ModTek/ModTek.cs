using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech.Data;
using ModTek.Features.AssembliesLoader;
using ModTek.Features.CustomDebugSettings;
using ModTek.Features.CustomSoundBankDefs;
using ModTek.Features.Logging;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.Mods;
using ModTek.Misc;
using ModTek.UI;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek;

public static partial class ModTek
{
    // ok fields
    internal static Configuration Config;

    // TBD fields below

    internal static ModDefEx SettingsDef { get; private set; }

    internal static bool Enabled => SettingsDef.Enabled;

    internal const string MODTEK_DEF_NAME = "ModTek";
    internal const string MOD_STATE_JSON_NAME = "modstate.json";

    private static Stopwatch stopwatch = new();

    // Called by assembly modified with ModTekPreloader via Doorstop
    public static void Init()
    {
        Load();
    }

    private static bool s_hasTriedLoading;
    private static void Load()
    {
        if (s_hasTriedLoading)
        {
            return;
        }
        s_hasTriedLoading = true;

        try
        {
            Start();
        }
        catch (Exception e)
        {
            LoggingFeature.WriteExceptionToFatalLog(e);
            FinishAndCleanup();
        }
    }

    private static void Start() {
        stopwatch.Start();

        Config = Configuration.FromDefaultFile();
        LoggingFeature.Init();
        Config.LogAnyDanglingExceptions();

        if (File.Exists(FilePaths.ModTekSettingsPath))
        {
            try
            {
                SettingsDef = ModDefEx.CreateFromPath(FilePaths.ModTekSettingsPath);
                SettingsDef.Version = GitVersionInformation.SemVer;
            }
            catch (Exception e)
            {
                throw new Exception($"Caught exception while parsing {FilePaths.ModTekSettingsPath}", e);
            }
        }

        if (SettingsDef == null)
        {
            Log.Main.Info?.Log("File not exists " + FilePaths.ModTekSettingsPath + " fallback to defaults");
            SettingsDef = new ModDefEx
            {
                Enabled = true,
                PendingEnable = true,
                Name = MODTEK_DEF_NAME,
                Version = GitVersionInformation.SemVer,
                Description = "Mod system for HBS's PC game BattleTech.",
                Author = "Mpstark, CptMoore, Tyler-IN, alexbartlow, janxious, m22spencer, KMiSSioN, ffaristocrat, Morphyum",
                Website = "https://github.com/BattletechModders/ModTek"
            };
            File.WriteAllText(FilePaths.ModTekSettingsPath, JsonConvert.SerializeObject(SettingsDef, Formatting.Indented));
            SettingsDef.Directory = FilePaths.ModTekDirectory;
            SettingsDef.SaveState();
        }

        // load progress bar
        if (Enabled && !ProgressPanel.Initialize(FilePaths.ModTekDirectory, $"ModTek v{GitVersionInformation.FullSemVer}"))
        {
            throw new Exception("Failed to load progress bar.  Skipping mod loading completely.");
        }

        try
        {
            Log.Main.Info?.Log("Applying ModTek harmony patches");
            Harmony.CreateAndPatchAll(typeof(ModTek).Assembly, "io.github.mpstark.ModTek");
        }
        catch (Exception e)
        {
            throw new Exception("PATCHING FAILED!", e);
        }

        if (Enabled == false)
        {
            Log.Main.Info?.Log("ModTek not enabled");
            FinishAndCleanup();
            return;
        }

        ModDefExLoading.Setup();

        {
            var version = MetadataDatabase.Instance.ExecuteScalar<string>("select sqlite_version();");
            Log.Main.Info?.Log("SQLite version "+ version);
        }

        LoadUsingProgressPanel();
    }

    private static void LoadUsingProgressPanel()
    {
        ProgressPanel.SubmitWork(ModDefsDatabase.InitModsLoop);
        ProgressPanel.SubmitWork(ModsManifest.HandleModManifestsLoop);
        ProgressPanel.SubmitWork(SoundBanksFeature.SoundBanksProcessing);
        ProgressPanel.SubmitWork(CustomAssembliesLoader.AssembliesProcessing);
        ProgressPanel.SubmitWork(ModDefsDatabase.GatherDependencyTreeLoop);
        ProgressPanel.SubmitWork(FinishingLoadingMods);
        ProgressPanel.SubmitWork(HarmonySummaryAndFinish);
    }

    private static IEnumerator<ProgressReport> FinishingLoadingMods()
    {
        DebugSettingsFeature.LoadDebugSettings();

        var sliderText = "Finishing Loading Mods";
        yield return new ProgressReport(1, sliderText, "", true);
        Log.Main.Info?.Log(sliderText);
        ModDefsDatabase.FinishedLoadingMods();
    }

    internal static IEnumerator<ProgressReport> HarmonySummaryAndFinish()
    {
        var sliderText = "Saving list of loaded assemblies";
        yield return new ProgressReport(1, sliderText, "", true);
        File.WriteAllText(
            FilePaths.AssembliesLoadedLogPath,
            "Assemblies loaded:" + AppDomain.CurrentDomain.GetAssemblies()
                .Select(AssemblyUtil.GetLocationOrName)
                .OrderBy(a => a)
                .AsTextList()
        );

        yield return new ProgressReport(1, "Game now loading", "", true);
        FinishAndCleanup();
    }

    internal static void FinishAndCleanup()
    {
        stopwatch.Stop();
        Log.Main.Info?.Log($"Done. Elapsed running time: {stopwatch.Elapsed.TotalSeconds} seconds");
        stopwatch = null;
    }
}