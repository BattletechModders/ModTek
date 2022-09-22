using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BattleTech.Data;
using ModTek.Features.CustomDebugSettings;
using ModTek.Features.CustomSoundBankDefs;
using ModTek.Features.HarmonyPatching;
using ModTek.Features.Logging;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.Mods;
using ModTek.Misc;
using ModTek.UI;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek
{
    public static partial class ModTek
    {
        // ok fields
        internal static Configuration Config;

        // TBD fields below

        internal static bool HasLoaded { get; private set; }

        internal static ModDefEx SettingsDef { get; private set; }

        internal static bool Enabled => SettingsDef.Enabled;

        internal const string MODTEK_DEF_NAME = "ModTek";
        internal const string MOD_STATE_JSON_NAME = "modstate.json";

        private static Stopwatch stopwatch = new Stopwatch();

        // Called by assembly modified with ModTekPreloader via Doorstop
        public static void Init()
        {
            Load();
        }

        private static void Load()
        {
            if (HasLoaded)
            {
                return;
            }

            try
            {
                Start();
            }
            catch (Exception e)
            {
                // Mods/.modtek already exist due to preloader
                File.WriteAllText(Path.Combine("Mods", ".modtek", "ModTekFatalError.log"), e.ToString());
            }
        }

        private static void Start() {
            stopwatch.Start();

            MTUnityUtils.Init();
            Config = Configuration.FromDefaultFile();
            LoggingFeature.Init();
            Config.LogAnyDanglingExceptions();

            if (File.Exists(FilePaths.ModTekSettingsPath))
            {
                try
                {
                    SettingsDef = ModDefEx.CreateFromPath(FilePaths.ModTekSettingsPath);
                    SettingsDef.Version = VersionTools.ShortVersion;
                }
                catch (Exception e)
                {
                    MTLogger.Error.Log($"Caught exception while parsing {FilePaths.ModTekSettingsPath}", e);
                    FinishAndCleanup();
                    return;
                }
            }

            if (SettingsDef == null)
            {
                MTLogger.Info.Log("File not exists " + FilePaths.ModTekSettingsPath + " fallback to defaults");
                SettingsDef = new ModDefEx
                {
                    Enabled = true,
                    PendingEnable = true,
                    Name = MODTEK_DEF_NAME,
                    Version = VersionTools.ShortVersion,
                    Description = "Mod system for HBS's PC game BattleTech.",
                    Author = "Mpstark, CptMoore, Tyler-IN, alexbartlow, janxious, m22spencer, KMiSSioN, ffaristocrat, Morphyum",
                    Website = "https://github.com/BattletechModders/ModTek"
                };
                File.WriteAllText(FilePaths.ModTekSettingsPath, JsonConvert.SerializeObject(SettingsDef, Formatting.Indented));
                SettingsDef.Directory = FilePaths.ModTekDirectory;
                SettingsDef.SaveState();
            }

            // load progress bar
            if (Enabled && !ProgressPanel.Initialize(FilePaths.ModTekDirectory, $"ModTek v{VersionTools.ShortVersion}"))
            {
                MTLogger.Error.Log("Failed to load progress bar.  Skipping mod loading completely.");
                FinishAndCleanup();
            }

            try
            {
                HarmonyUtils.PatchAll();
            }
            catch (Exception e)
            {
                MTLogger.Error.Log("PATCHING FAILED!", e);
                return;
            }

            if (Enabled == false)
            {
                MTLogger.Info.Log("ModTek not enabled");
                FinishAndCleanup();
                return;
            }

            ModDefExLoading.Setup();

            {
                var version = MetadataDatabase.Instance.ExecuteScalar<string>("select sqlite_version();");
                MTLogger.Info.Log("SQLite version "+ version);
            }

            LoadUsingProgressPanel();
        }

        private static void LoadUsingProgressPanel()
        {
            ProgressPanel.SubmitWork(ModDefsDatabase.InitModsLoop);
            ProgressPanel.SubmitWork(ModsManifest.HandleModManifestsLoop);
            ProgressPanel.SubmitWork(SoundBanksFeature.SoundBanksProcessing);
            ProgressPanel.SubmitWork(ModDefsDatabase.GatherDependencyTreeLoop);
            ProgressPanel.SubmitWork(FinishingLoadingMods);
            ProgressPanel.SubmitWork(HarmonySummaryAndFinish);
        }

        private static IEnumerator<ProgressReport> FinishingLoadingMods()
        {
            DebugSettingsFeature.LoadDebugSettings();

            var sliderText = "Finishing Loading Mods";
            yield return new ProgressReport(1, sliderText, "", true);
            MTLogger.Info.Log(sliderText);
            ModDefsDatabase.FinishedLoadingMods();
        }

        internal static IEnumerator<ProgressReport> HarmonySummaryAndFinish()
        {
            var sliderText = "Saving Harmony Summary";
            yield return new ProgressReport(1, sliderText, "", true);
            MTLogger.Info.Log(sliderText);
            HarmonyUtils.PrintHarmonySummary();

            MTLogger.Info.Log("Assemblies loaded:" + AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => AssemblyUtil.GetLocationOrName(a))
                .OrderBy(a => a)
                .AsTextList()
            );

            yield return new ProgressReport(1, "Game now loading", "", true);
            FinishAndCleanup();
        }

        internal static void FinishAndCleanup()
        {
            HasLoaded = true;

            stopwatch.Stop();
            MTLogger.Info.Log($"Done. Elapsed running time: {stopwatch.Elapsed.TotalSeconds} seconds");
            stopwatch = null;
        }
    }
}
