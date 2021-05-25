using BattleTech;
using Harmony;
using ModTek.UI;
using ModTek.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ModTek.Logging;
using ModTek.Manifest;
using ModTek.Manifest.MDD;
using ModTek.Manifest.Merges;
using ModTek.Misc;
using ModTek.Mods;
using ModTek.SoundBanks;
using static ModTek.Logging.Logger;

namespace ModTek
{
    public static class ModTek
    {
        // ok fields
        internal static Configuration Config;

        // TBD fields below

        internal static bool HasLoaded { get; private set; }

        internal static ModDefEx SettingsDef { get; private set; }

        internal static bool Enabled => SettingsDef.Enabled;

        internal const string MODTEK_DEF_NAME = "ModTek";
        internal const string MOD_STATE_JSON_NAME = "modstate.json";

        // special StreamingAssets relative directories

        // internal temp structures
        private static System.Diagnostics.Stopwatch stopwatch = new();

        // the end result of loading mods, these are used to push into game data through patches

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

            ModDefExLoading.Setup();

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

            ModsManifest.PrepareManifestAndCustomResources();
            LoadMods();
        }

        internal static void Finish()
        {
            HasLoaded = true;

            stopwatch.Stop();
            Log("");
            LogWithDate($"Done. Elapsed running time: {stopwatch.Elapsed.TotalSeconds} seconds\n");

            CloseLogStream();

            JObjectCache.ClearTemp();
            ModsManifest.ClearTemp();
            stopwatch = null;
        }

        // PATHS

        private static void LoadMods()
        {
            ProgressPanel.SubmitWork(ModDefsDatabase.InitModsLoop);
            ProgressPanel.SubmitWork(ModsManifest.ProcessLoop);
            ProgressPanel.SubmitWork(SoundBanksFeature.SoundBanksProcessing);
            ProgressPanel.SubmitWork(ModDefsDatabase.GatherDependencyTreeLoop);
            ProgressPanel.SubmitWork(FinishLoop);
        }

        private static IEnumerator<ProgressReport> FinishLoop()
        {
            // "Loop"
            yield return new ProgressReport(1, "Finishing Up", "", true);
            Log("\nFinishing Up");

            ModsManifest.FinalizeResourceLoading();

            ModDefExLoading.FinishedLoadingMods();

            Config?.ToFile(FilePaths.ConfigPath);

            Finish();
        }
    }
}
