using System;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.BTRL;
using static ModTek.Logging.Logger;

namespace ModTek.Features.CustomStreamingAssets
{
    // custom streaming assets don't require the specification of a type
    // custom resources do require a type
    internal static class CustomStreamingAssetsFeature
    {
        private enum CSAType
        {
            DebugSettings,
            GameTip
        }

        internal static readonly string[] CSATypeNames = Enum.GetNames(typeof(CSAType));

        internal static bool IsCustomStreamingAssetsType(string type)
        {
            return type != null && CSATypeNames.Contains(type);
        }

        internal static void LoadDebugSettings()
        {
            DebugBridge.LoadDefaultSettings();
        }

        public static string GetDebugSettings()
        {
            var entry = BetterBTRL.Instance.EntryByIDAndType("settings", CSAType.DebugSettings.ToString());
            Log($"Debug settings: {entry.FilePath}");
            return ModsManifest.GetMergedContentOrReadAllTextAndMerge(entry);
        }

        internal static string GetGameTip(string path)
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var entry = BetterBTRL.Instance.EntryByIDAndType(id, CSAType.GameTip.ToString());
            return ModsManifest.GetMergedContentOrReadAllTextAndMerge(entry);
        }

        internal static readonly VersionManifestEntry[] DefaultCustomStreamingAssets =
        {
            new(
                "settings",
                Path.Combine(Path.Combine("data", "debug"), "settings.json"),
                CSAType.DebugSettings.ToString(),
                DateTime.MinValue,
                "1"
            ),
            new(
                "general",
                Path.Combine("GameTips", "general.txt"),
                CSAType.GameTip.ToString(),
                DateTime.MinValue,
                "1"
            ),
            new(
                "combat",
                Path.Combine("GameTips", "combat.txt"),
                CSAType.GameTip.ToString(),
                DateTime.MinValue,
                "1"
            ),
            new(
                "lore",
                Path.Combine("GameTips", "lore.txt"),
                CSAType.GameTip.ToString(),
                DateTime.MinValue,
                "1"
            ),
            new(
                "sim",
                Path.Combine("GameTips", "sim.txt"),
                CSAType.GameTip.ToString(),
                DateTime.MinValue,
                "1"
            )
        };

        internal static void FindAndSetMatchingCustomStreamingAssetsType(ModEntry entry)
        {
            if (entry.Type != null || !entry.IsStreamingAssetsMergesBasePath)
            {
                return;
            }

            switch (entry.Id)
            {
                case "settings":
                    entry.Type = CSAType.DebugSettings.ToString();
                    break;
                case "general":
                case "combat":
                case "lore":
                case "sim":
                    entry.Type = CSAType.GameTip.ToString();
                    break;
            }
        }
    }
}
