using System.Collections.Generic;
using BattleTech.UI;
using ModTek.Features.Manifest;

namespace ModTek.Features.CustomSVGAssets
{
    internal static class SVGAssetFeature
    {
        private static HashSet<string> systemIcons = new HashSet<string>();

        internal static bool isInSystemIcons(string id)
        {
            return systemIcons.Contains(id);
        }

        public static void OnAddSVGEntry(ModEntry entry)
        {
            if (entry.Id.StartsWith(nameof(UILookAndColorConstants)))
            {
                systemIcons.Add(entry.Id);
            }
        }
    }
}
