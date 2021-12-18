﻿using System.IO;
using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.Logging;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;

namespace ModTek.Features.CustomDebugSettings
{
    internal static class DebugSettingsFeature
    {
        internal static void LoadDebugSettings()
        {
            DebugBridge.LoadDefaultSettings();
        }

        public static string GetDebugSettings()
        {
            var entry = BetterBTRL.Instance.EntryByIDAndType("settings", InternalCustomResourceType.DebugSettings.ToString());
            MTLogger.Log($"Debug settings: {entry.FilePath}");
            return ModsManifest.GetJson(entry);
        }

        internal static readonly VersionManifestEntry[] DefaulManifestEntries =
        {
            new VersionManifestEntry(
                "settings",
                Path.Combine(Path.Combine("data", "debug"), "settings.json"),
                InternalCustomResourceType.DebugSettings.ToString(),
                VersionManifestEntryExtensions.UpdatedOnLazyTracking,
                "1"
            )
        };

        internal static bool FindAndSetMatchingType(ModEntry entry)
        {
            switch (entry.Id)
            {
                case "settings":
                    entry.Type = InternalCustomResourceType.DebugSettings.ToString();
                    return true;
            }

            return false;
        }
    }
}
