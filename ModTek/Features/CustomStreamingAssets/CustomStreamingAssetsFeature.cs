using System;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.BTRL;
using static ModTek.Features.Logging.MTLogger;

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
            var entry = BetterBTRL.Instance.EntryByIDAndType("settings", CustomStreamingAssetsType.DebugSettings.ToString());
            Log($"Debug settings: {entry.FilePath}");
            return ModsManifest.GetJson(entry);
        }

        internal static string GetGameTip(string path)
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var entry = BetterBTRL.Instance.EntryByIDAndType(id, CustomStreamingAssetsType.GameTip.ToString());
            return ModsManifest.GetJson(entry);
        }

        internal static readonly VersionManifestEntry[] DefaultCustomStreamingAssets =
        {
            new VersionManifestEntry(
                "settings",
                Path.Combine(Path.Combine("data", "debug"), "settings.json"),
                CustomStreamingAssetsType.DebugSettings.ToString(),
                DateTime.MinValue,
                "1"
            ),
            new VersionManifestEntry(
                "general",
                Path.Combine("GameTips", "general.txt"),
                CustomStreamingAssetsType.GameTip.ToString(),
                DateTime.MinValue,
                "1"
            ),
            new VersionManifestEntry(
                "combat",
                Path.Combine("GameTips", "combat.txt"),
                CustomStreamingAssetsType.GameTip.ToString(),
                DateTime.MinValue,
                "1"
            ),
            new VersionManifestEntry(
                "lore",
                Path.Combine("GameTips", "lore.txt"),
                CustomStreamingAssetsType.GameTip.ToString(),
                DateTime.MinValue,
                "1"
            ),
            new VersionManifestEntry(
                "sim",
                Path.Combine("GameTips", "sim.txt"),
                CustomStreamingAssetsType.GameTip.ToString(),
                DateTime.MinValue,
                "1"
            )
        };

        internal static bool FindAndSetMatchingCustomStreamingAssetsType(ModEntry entry)
        {
            switch (entry.Id)
            {
                case "settings":
                    entry.Type = CustomStreamingAssetsType.DebugSettings.ToString();
                    return true;
                case "general":
                case "combat":
                case "lore":
                case "sim":
                    entry.Type = CustomStreamingAssetsType.GameTip.ToString();
                    return true;
            }

            return false;
        }
    }
}
