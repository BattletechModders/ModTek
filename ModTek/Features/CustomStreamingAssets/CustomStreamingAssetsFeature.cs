using System;
using System.IO;
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
        internal static void LoadDebugSettings()
        {
            DebugBridge.LoadDefaultSettings();
        }

        public static string GetDebugSettings()
        {
            var entry = BetterBTRL.Instance.CustomEntryByID("settings", BTConstants.CustomType_DebugSettings);
            Log($"debug settings entry: {entry}");
            return ModsManifest.GetMergedContentOrReadAllTextAndMerge(entry);
        }

        internal static string GetGameTip(string path)
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var entry = BetterBTRL.Instance.CustomEntryByID(id, BTConstants.CustomType_GameTip);
            return ModsManifest.GetMergedContentOrReadAllTextAndMerge(entry);
        }

        internal static readonly VersionManifestEntry[] DefaultCustomStreamingAssets =
        {
            new(
                "settings",
                Path.Combine(Path.Combine("data", "debug"), "settings.json"),
                BTConstants.CustomType_DebugSettings,
                DateTime.MinValue,
                "1"
            ),
            new(
                "general",
                Path.Combine("GameTips", "general.txt"),
                BTConstants.CustomType_GameTip,
                DateTime.MinValue,
                "1"
            ),
            new(
                "combat",
                Path.Combine("GameTips", "combat.txt"),
                BTConstants.CustomType_GameTip,
                DateTime.MinValue,
                "1"
            ),
            new(
                "lore",
                Path.Combine("GameTips", "lore.txt"),
                BTConstants.CustomType_GameTip,
                DateTime.MinValue,
                "1"
            ),
            new(
                "sim",
                Path.Combine("GameTips", "sim.txt"),
                BTConstants.CustomType_GameTip,
                DateTime.MinValue,
                "1"
            )
        };

        internal static void NormalizedModEntry(ModEntry entry)
        {
            if (!entry.IsStreamingAssetsMergesBasePath || entry.Type != null)
            {
                return;
            }

            switch (entry.Id)
            {
                case "settings":
                    entry.Type = BTConstants.CustomType_DebugSettings;
                    break;
                case "general":
                case "combat":
                case "lore":
                case "sim":
                    entry.Type = BTConstants.CustomType_GameTip;
                    break;
            }
        }
    }
}
