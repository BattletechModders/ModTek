using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Harmony;
using ModTek.Features.CustomResources;
using ModTek.Features.CustomStreamingAssets;
using ModTek.Features.CustomSVGAssets.Patches;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.Mods;
using ModTek.Features.SoundBanks;
using ModTek.Logging;
using ModTek.Misc;
using ModTek.UI;
using Newtonsoft.Json;
using static ModTek.Logging.Logger;

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

        // internal temp structures
        private static Stopwatch stopwatch = new();

        // the end result of loading mods, these are used to push into game data through patches

        // INITIALIZATION (called by injected code)
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

            FilePaths.SetupPaths();
            LogInit();
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
                Log($"ModTek v{version} -- {DateTime.Now}");
                Start(version);
            }
            catch (Exception e)
            {
                Log("Fatal error", e);
            }
        }

        private static void Start(string version) {
            stopwatch.Start();

            // creates the directories above it as well
            Directory.CreateDirectory(FilePaths.MergeCacheDirectory);
            Directory.CreateDirectory(FilePaths.MDDBCacheDirectory);

            RLog.InitLog(FilePaths.TempModTekDirectory, true);
            RLog.M.TWL(0, "Init ModTek version " + Assembly.GetExecutingAssembly().GetName().Version);
            if (File.Exists(FilePaths.ChangedFlagPath))
            {
                File.Delete(FilePaths.ChangedFlagPath);
                ModTekCacheStorage.CleanModTekTempDir(new DirectoryInfo(FilePaths.TempModTekDirectory));
                Directory.CreateDirectory(FilePaths.MergeCacheDirectory);
                Directory.CreateDirectory(FilePaths.MDDBCacheDirectory);
            }

            if (File.Exists(FilePaths.ModTekSettingsPath))
            {
                try
                {
                    SettingsDef = ModDefEx.CreateFromPath(FilePaths.ModTekSettingsPath);
                    SettingsDef.Version = version;
                }
                catch (Exception e)
                {
                    Log($"Error: Caught exception while parsing {FilePaths.ModTekSettingsPath}", e);
                    Finish();
                    return;
                }
            }

            if (SettingsDef == null)
            {
                Log("File not exists " + FilePaths.ModTekSettingsPath + " fallback to defaults");
                SettingsDef = new ModDefEx
                {
                    Enabled = true,
                    PendingEnable = true,
                    Name = MODTEK_DEF_NAME,
                    Version = version,
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
                if (!ProgressPanel.Initialize(FilePaths.ModTekDirectory, $"ModTek v{version}"))
                {
                    Log("Error: Failed to load progress bar.  Skipping mod loading completely.");
                    Finish();
                }
            }

            // read config
            Config = Configuration.FromFile(FilePaths.ConfigPath);

            ModDefExLoading.Setup();

            try
            {
                var instance = HarmonyInstance.Create("io.github.mpstark.ModTek");
                instance.PatchAll(Assembly.GetExecutingAssembly());
                //BattleTechResourceLoader.Refresh();
                SVGAssetLoadRequest_Load.Patch(instance);
            }
            catch (Exception e)
            {
                Log("Error: PATCHING FAILED!", e);
                return;
            }

            if (Enabled == false)
            {
                Log("ModTek not enabled");
                return;
            }

            // UnityGameInstance.BattleTechGame.DataManager.HeatSinkDefs.TryGet("Gear_HeatSink_Generic_Standard")

            CustomResourcesFeature.Setup();
            LoadMods();
        }

        private static void LoadMods()
        {
            ProgressPanel.SubmitWork(ModDefsDatabase.InitModsLoop);
            ProgressPanel.SubmitWork(ModsManifest.HandleModManifestsLoop);
            ProgressPanel.SubmitWork(SoundBanksFeature.SoundBanksProcessing);
            ProgressPanel.SubmitWork(ModDefsDatabase.GatherDependencyTreeLoop);
            ProgressPanel.SubmitWork(FinishInitialLoadingLoop);
        }

        private static IEnumerator<ProgressReport> FinishInitialLoadingLoop()
        {
            yield return new ProgressReport(1, "Finishing Up", "", true);
            Log("\nFinishing Up");

            CustomStreamingAssetsFeature.LoadDebugSettings();
            ModDefsDatabase.FinishedLoadingMods();

            Config?.ToFile(FilePaths.ConfigPath);

            Finish();
        }

        internal static void Finish()
        {
            HasLoaded = true;

            stopwatch.Stop();
            Log("");
            LogWithDate($"Done. Elapsed running time: {stopwatch.Elapsed.TotalSeconds} seconds\n");
            stopwatch = null;
        }
    }
}
