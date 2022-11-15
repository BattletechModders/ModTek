using System.Collections.Generic;
using BattleTech.UI;

namespace ModTek.Features.CustomSVGAssets;

internal static class SVGAssetFeature
{
    private static HashSet<string> systemIcons = new();

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