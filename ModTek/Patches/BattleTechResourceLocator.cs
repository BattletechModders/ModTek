using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch RefreshTypedEntries to add mod resources directly to its internal dictionary
    /// This is instead of patching the VersionManifest, which is skipped for content from DLCs
    /// </summary>
    [HarmonyPatch(typeof(BattleTechResourceLocator), "RefreshTypedEntries")]
    public static class BattleTechResourceLocator_RefreshTypedEntries_Patch
    {
        public static void Postfix(ContentPackIndex ___contentPackIndex,
            Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>> ___baseManifest,
            Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>> ___contentPacksManifest,
            Dictionary<VersionManifestAddendum, Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>>> ___addendumsManifest)
        {
            foreach (var entry in ModTek.AddBTRLEntries)
            {
                var versionManifestEntry = entry.GetVersionManifestEntry();
                var resourceType = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), entry.Type);

                if (___contentPackIndex == null || ___contentPackIndex.IsResourceOwned(entry.Id))
                {
                    // add to baseManifest
                    if (!___baseManifest.ContainsKey(resourceType))
                        ___baseManifest.Add(resourceType, new Dictionary<string, VersionManifestEntry>());

                    ___baseManifest[resourceType][entry.Id] = versionManifestEntry;
                }
                else
                {
                    // add to contentPackManifest
                    if (!___contentPacksManifest.ContainsKey(resourceType))
                        ___contentPacksManifest.Add(resourceType, new Dictionary<string, VersionManifestEntry>());

                    ___contentPacksManifest[resourceType][entry.Id] = versionManifestEntry;
                }

                if (string.IsNullOrEmpty(entry.AddToAddendum))
                    continue;

                // add to addendumsManifest
                var addendum = ModTek.CachedVersionManifest.GetAddendumByName(entry.AddToAddendum);
                if (addendum != null)
                {
                    if (!___addendumsManifest.ContainsKey(addendum))
                        ___addendumsManifest.Add(addendum, new Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>>());

                    if (!___addendumsManifest[addendum].ContainsKey(resourceType))
                        ___addendumsManifest[addendum].Add(resourceType, new Dictionary<string, VersionManifestEntry>());

                    ___addendumsManifest[addendum][resourceType][entry.Id] = versionManifestEntry;
                }
            }

            foreach (var entry in ModTek.RemoveBTRLEntries)
            {
                var resourceType = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), entry.Type);

                if (___baseManifest.ContainsKey(resourceType) && ___baseManifest[resourceType].ContainsKey(entry.Id))
                    ___baseManifest[resourceType].Remove(entry.Id);

                if (___contentPacksManifest.ContainsKey(resourceType) && ___contentPacksManifest[resourceType].ContainsKey(entry.Id))
                    ___contentPacksManifest[resourceType].Remove(entry.Id);

                var containingAddendums = ___addendumsManifest.Where(pair => pair.Value.ContainsKey(resourceType) && pair.Value[resourceType].ContainsKey(entry.Id));
                foreach (var containingAddendum in containingAddendums)
                    containingAddendum.Value[resourceType].Remove(entry.Id);
            }
        }
    }
}
