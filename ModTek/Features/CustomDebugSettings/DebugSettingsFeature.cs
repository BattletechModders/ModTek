using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;
using ModTek.Misc;

namespace ModTek.Features.CustomDebugSettings;

internal static class DebugSettingsFeature
{
    internal static void LoadDebugSettings()
    {
        DebugBridge.LoadDefaultSettings();
    }

    internal static readonly VersionManifestEntry[] DefaultManifestEntries;
    static DebugSettingsFeature()
    {
        var baseDir = Path.Combine("data", "debug");
        var files = Directory.GetFiles(Path.Combine(FilePaths.StreamingAssetsDirectory, baseDir), "*.json");
        DefaultManifestEntries = files
            .Select(file =>
                new VersionManifestEntry(
                    Path.GetFileNameWithoutExtension(file),
                    Path.Combine(baseDir, file),
                    InternalCustomResourceType.DebugSettings.ToString(),
                    VersionManifestEntryExtensions.UpdatedOnLazyTracking,
                    "1"
                )
            )
            .ToArray();
    }

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

    internal static bool TryGetSettingsPath(string id, out string path)
    {
        var entry = BetterBTRL.Instance.EntryByIDAndType(id, InternalCustomResourceType.DebugSettings.ToString());
        if (entry == null)
        {
            path = default;
            return false;
        }
        Log.Main.Info?.Log($"Debug settings: {entry.FilePath}");
        path = entry.FilePath;
        return true;
    }
}