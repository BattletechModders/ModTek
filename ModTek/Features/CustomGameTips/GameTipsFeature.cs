using System.IO;
using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;

namespace ModTek.Features.CustomGameTips
{
    // custom streaming assets add existing resources to BTRL
    internal static class GameTipsFeature
    {
        internal static string GetGameTip(string path)
        {
            var id = Path.GetFileNameWithoutExtension(path);
            var entry = BetterBTRL.Instance.EntryByIDAndType(id, InternalCustomResourceType.GameTip.ToString());
            return ModsManifest.GetText(entry);
        }

        internal static readonly VersionManifestEntry[] DefaulManifestEntries =
        {
            new VersionManifestEntry(
                "general",
                Path.Combine("GameTips", "general.txt"),
                InternalCustomResourceType.GameTip.ToString(),
                VersionManifestEntryExtensions.UpdatedOnLazyTracking,
                "1"
            ),
            new VersionManifestEntry(
                "combat",
                Path.Combine("GameTips", "combat.txt"),
                InternalCustomResourceType.GameTip.ToString(),
                VersionManifestEntryExtensions.UpdatedOnLazyTracking,
                "1"
            ),
            new VersionManifestEntry(
                "lore",
                Path.Combine("GameTips", "lore.txt"),
                InternalCustomResourceType.GameTip.ToString(),
                VersionManifestEntryExtensions.UpdatedOnLazyTracking,
                "1"
            ),
            new VersionManifestEntry(
                "sim",
                Path.Combine("GameTips", "sim.txt"),
                InternalCustomResourceType.GameTip.ToString(),
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
                case "general":
                case "combat":
                case "lore":
                case "sim":
                    entry.Type = InternalCustomResourceType.GameTip.ToString();
                    return true;
            }

            return false;
        }
    }
}
