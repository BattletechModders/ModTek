using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using ModTek.Features.CustomResources;
using ModTek.Features.CustomStreamingAssets;
using ModTek.Features.CustomSVGAssets.Patches;
using ModTek.Features.Logging;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.Mods;
using ModTek.Features.SoundBanks;
using ModTek.Misc;
using ModTek.UI;
using ModTek.Util;
using Newtonsoft.Json;
using static ModTek.Features.Logging.MTLogger;

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

            try
            {
                Start();
            }
            catch (Exception e)
            {
                Log("Fatal error", e);
            }
        }

        private static void Start() {
            stopwatch.Start();

            FilePaths.SetupPaths();
            Config = Configuration.FromDefaultFile();
            LoggingFeature.Init();

            if (File.Exists(FilePaths.ModTekSettingsPath))
            {
                try
                {
                    SettingsDef = ModDefEx.CreateFromPath(FilePaths.ModTekSettingsPath);
                    SettingsDef.Version = VersionTools.ShortVersion;
                }
                catch (Exception e)
                {
                    Log($"Error: Caught exception while parsing {FilePaths.ModTekSettingsPath}", e);
                    FinishAndCleanup();
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
                Log("Error: Failed to load progress bar.  Skipping mod loading completely.");
                FinishAndCleanup();
            }

            if (File.Exists(FilePaths.ChangedFlagPath))
            {
                File.Delete(FilePaths.ChangedFlagPath);
                ModTekCacheStorage.CleanModTekTempDir(new DirectoryInfo(FilePaths.TempModTekDirectory));
                Directory.CreateDirectory(FilePaths.MergeCacheDirectory);
                Directory.CreateDirectory(FilePaths.MDDBCacheDirectory);
            }

            LogIf(Config.AssembliesToPreload.Length > 0, "Preloading assemblies");
            foreach (var assemblyToPreload in ModTek.Config.AssembliesToPreload)
            {
                Log($"\tLoading {assemblyToPreload}");
                var assemblyPath = Path.Combine(FilePaths.ModsDirectory, assemblyToPreload);
                if (!File.Exists(assemblyPath))
                {
                    Log($"\t\tWarning: Can't find assembly at {assemblyPath}, aborting load.");
                    continue;
                }

                try
                {
                    AssemblyUtil.LoadDLL(assemblyPath);
                }
                catch (Exception e)
                {
                    Log($"\t\tError: Failed to preload the assembly.", e);
                }
            }

            try
            {
                var instance = HarmonyInstance.Create("io.github.mpstark.ModTek");
                PatchAll(instance, Assembly.GetExecutingAssembly());
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
                FinishAndCleanup();
                return;
            }

            ModDefExLoading.Setup();
            CustomResourcesFeature.Setup();

            LoadUsingProgressPanel();
        }

        private static void PatchAll(HarmonyInstance instance, Assembly assembly) => assembly.GetTypes().Do(type =>
        {
            try
            {
                var harmonyMethods = type.GetHarmonyMethods();
                if (harmonyMethods == null || !harmonyMethods.Any())
                    return;
                var attributes = HarmonyMethod.Merge(harmonyMethods);
                new PatchProcessor(instance, type, attributes).Patch();
            }
            catch (Exception)
            {
                Log($"Applying patch {type} failed");
                throw;
            }
        });

        private static void LoadUsingProgressPanel()
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
            Log("Finishing Up");

            CustomStreamingAssetsFeature.LoadDebugSettings();
            ModDefsDatabase.FinishedLoadingMods();

            FinishAndCleanup();
        }

        internal static void FinishAndCleanup()
        {
            HasLoaded = true;

            stopwatch.Stop();
            Log($"Done. Elapsed running time: {stopwatch.Elapsed.TotalSeconds} seconds");
            stopwatch = null;
        }
    }
}
