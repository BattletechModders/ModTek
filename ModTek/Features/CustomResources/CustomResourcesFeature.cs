using System;
using System.Collections.Generic;
using System.Linq;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.BTRL;
using CustomResourcesDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, BattleTech.VersionManifestEntry>>;

namespace ModTek.Features.CustomResources;

internal static class CustomResourcesFeature
{
    private static readonly HashSet<string> CustomResources = Enum.GetNames(typeof(InternalCustomResourceType)).ToHashSet();

    internal static IReadOnlyCollection<string> GetTypes()
    {
        return CustomResources;
    }

    internal static bool IsCustomResourceType(string type)
    {
        return CustomResources.Contains(type);
    }

    internal static void ProcessModDef(ModDefEx modDef)
    {
        foreach (var customResourceType in modDef.CustomResourceTypes)
        {
            if (BTConstants.PREDEFINED_TYPES.Contains(customResourceType))
            {
                Log.Main.Warning?.Log($"\t{modDef.QuotedName} has custom resource type '{customResourceType}' that has the same name as a vanilla/modtek resource type. Ignoring this type.");
                continue;
            }

            if (CustomResources.Add(customResourceType))
            {
                Log.Main.Info?.Log($"\tAdded mod custom resource type '{customResourceType}'.");
            }
        }
    }

    internal static CustomResourcesDict GetResourceDictionariesForTypes(IEnumerable<string> modDefCustomResourceTypes)
    {
        return modDefCustomResourceTypes
            .ToDictionary(
                resourceType => resourceType,
                resourceType => BetterBTRL.Instance.AllEntriesOfType(resourceType).ToDictionary(e => e.Id)
            );
    }
}