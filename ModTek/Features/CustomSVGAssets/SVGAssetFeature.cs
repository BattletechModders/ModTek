using System.Collections.Generic;
using BattleTech.UI;
using ModTek.Features.Manifest;
using SVGImporter;
using static ModTek.Logging.Logger;

namespace ModTek.Features.CustomSVGAssets
{
    internal static class SVGAssetFeature
    {
        private static HashSet<string> systemIcons = new();

        internal static bool isInSystemIcons(string id)
        {
            return systemIcons.Contains(id);
        }

        public static void OnAddSVGEntry(ModEntry entry)
        {
            Log($"Processing SVG entry of: {entry.Id}  type: {entry.Type}  name: {nameof(SVGAsset)}  path: {entry.RelativePathToMods}");
            if (entry.Id.StartsWith(nameof(UILookAndColorConstants)))
            {
                systemIcons.Add(entry.Id);
            }
        }
    }
}
