using BattleTech;
using BattleTech.Data;
using Harmony;
using System;
using System.Collections.Generic;

namespace ModTek
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
            foreach (var entry in ModTek.BTRLEntries)
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

                if (!string.IsNullOrEmpty(entry.AddToAddendum))
                {
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
            }
        }
    }
}
