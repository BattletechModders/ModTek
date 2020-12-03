using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using Harmony;
using ModTek.RuntimeLog;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    [HarmonyPatch(typeof(BattleTechResourceLocator), "AllEntriesOfResourceFromAddendum")]
    public static class BattleTechResourceLocator_AllEntriesOfResourceFromAddendum_Patch
    {
        public static bool Prepare() { return ModTek.Enabled; }

        public static void Postfix(BattleTechResourceLocator __instance, BattleTechResourceType type, VersionManifestAddendum addendum, ref VersionManifestEntry[] __result)
        {
            if (ModTek.Config.EnableDebugLogging)
            {
                RLog.M.TWL(0, "BattleTechResourceLocator.AllEntriesOfResourceFromAddendum " + addendum.Name + " type:" + type);
                RLog.M.TWL(0, "BattleTechResourceLocator.AllEntriesOfResourceFromAddendum " + addendum.Name + " type:" + type);
                foreach (VersionManifestEntry entry in __result)
                {
                    RLog.M.WL(1, entry.FileName);
                }
            }
        }
    }
    [HarmonyPatch(typeof(BattleTechResourceLocator), "EntryByID")]
    public static class BattleTechResourceLocator_EntryByID_Patch
    {
        private static Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>> modtekManifest = new Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>>();

        public static bool Prepare() { return false; }
        public static void Postfix(string id, BattleTechResourceType type, bool filterByOwnership, ref VersionManifestEntry __result)
        {
            RLog.M.TWL(0, "BattleTechResourceLocator.EntryByID "+type+" "+id);
            if(__result == null)
            {
                RLog.M.WL(1, "can't find in internal manifest");
                if(modtekManifest.TryGetValue(type,out Dictionary<string,VersionManifestEntry> manifests))
                {
                    id = id.ToUpper(CultureInfo.InvariantCulture);
                    if (manifests.TryGetValue(id,out VersionManifestEntry entry))
                    {
                        __result = entry;
                        RLog.M.WL(1, "found in external manifest", true);
                    }
                    else
                    {
                        RLog.M.WL(1, "can't find in external manifest:"+manifests.Count);
                    }
                }
                else
                {

                }
            }
        }
    }
    /// <summary>
    /// Patch RefreshTypedEntries to add mod resources directly to its internal dictionary
    /// This is instead of patching the VersionManifest, which is skipped for content from DLCs
    /// </summary>
    [HarmonyPatch(typeof(BattleTechResourceLocator), "RefreshTypedEntries")]
    public static class BattleTechResourceLocator_RefreshTypedEntries_Patch
    {
        public static bool Prepare() { return ModTek.Enabled; }

        private static PropertyInfo f_manifest = typeof(BattleTechResourceLocator).GetProperty("manifest", BindingFlags.Instance|BindingFlags.NonPublic);
        private static VersionManifest manifest(this BattleTechResourceLocator locator) { return (VersionManifest)f_manifest.GetValue(locator); }
        private static void manifest(this BattleTechResourceLocator locator, VersionManifest manifest) { f_manifest.SetValue(locator, manifest); }
        public static void Prefix(BattleTechResourceLocator __instance, ref Stopwatch __state, int ___typedEntriesCount)
        {
            __state = new Stopwatch();
            __state.Start();

            VersionManifest versionManifest = __instance.manifest();
            if (ModTek.Config.EnableDebugLogging)
            {
                RLog.M.TWL(0, "BattleTechResourceLocator.RefreshTypedEntries");
                RLog.M.TWL(0, $"  typedEntriesCount: {___typedEntriesCount}  versionManifest.Count: {versionManifest.Count}");
                RLog.M.TWL(0, $"  addBTRLEntries: {ModTek.AddBTRLEntries.Count}  removeBTRLEntries: {ModTek.RemoveBTRLEntries.Count}");
            }

            
            if (versionManifest != ModTek.CachedVersionManifest)
            {
                RLog.M.TWL(0, "WARNING! STRANGE BEHAVIOR cachedManifest does not much locator manifest. Resolving");
                __instance.manifest(ModTek.CachedVersionManifest);
            }
        }

        public static void Postfix(BattleTechResourceLocator __instance, ContentPackIndex ___contentPackIndex,
            ref Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>> ___baseManifest,
            ref Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>> ___contentPacksManifest,
            ref Dictionary<VersionManifestAddendum, Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>>> ___addendumsManifest,
            Stopwatch __state)
        {

            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var entry in ModTek.AddBTRLEntries)
            {
                var versionManifestEntry = entry.GetVersionManifestEntry();
                var resourceType = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), entry.Type);
                // versionManifestEntry.AddToModTekManifest(resourceType); THIS WAS A NOP

                if (___contentPackIndex == null || ___contentPackIndex.IsResourceOwned(versionManifestEntry.Id))
                {
                    // add to baseManifest
                    Dictionary<string, VersionManifestEntry> manifests = null;
                    if (!___baseManifest.TryGetValue(resourceType, out manifests))
                    {
                        manifests = new Dictionary<string, VersionManifestEntry>();
                        ___baseManifest.Add(resourceType, manifests);
                    }
                    if (manifests.ContainsKey(versionManifestEntry.Id))
                    {
                        ___baseManifest[resourceType][versionManifestEntry.Id] = versionManifestEntry;
                    }
                    else
                    {
                        manifests.Add(versionManifestEntry.Id, versionManifestEntry);
                    }
                    
                    if (ModTek.Config.EnableDebugLogging)
                        RLog.M.WL(1, "baseManifest add:"+resourceType+" "+entry.Id+" "+ (versionManifestEntry == null?"null": versionManifestEntry.FilePath));
                }
                else
                {
                    // add to contentPackManifest
                    if (!___contentPacksManifest.ContainsKey(resourceType))
                        ___contentPacksManifest.Add(resourceType, new Dictionary<string, VersionManifestEntry>());

                    ___contentPacksManifest[resourceType][entry.Id] = versionManifestEntry;
                    if (ModTek.Config.EnableDebugLogging)
                        RLog.M.WL(1, "contentPacksManifest add:" + resourceType + " " + entry.Id);
                }

                if (string.IsNullOrEmpty(entry.AddToAddendum))
                    continue;

                // add to addendumsManifest
                var addendum = ModTek.CachedVersionManifest.GetAddendumByName(entry.AddToAddendum);
                //var addendum = __instance.GetAddendumByName(entry.AddToAddendum);
                if (addendum != null)
                {
                    if (!___addendumsManifest.ContainsKey(addendum))
                    {
                        if (ModTek.Config.EnableDebugLogging)
                            RLog.M.WL(1, "adding new enum:"+addendum.Name);
                        ___addendumsManifest.Add(addendum, new Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>>());
                    }
                    if (!___addendumsManifest[addendum].ContainsKey(resourceType))
                        ___addendumsManifest[addendum].Add(resourceType, new Dictionary<string, VersionManifestEntry>());

                    ___addendumsManifest[addendum][resourceType][entry.Id] = versionManifestEntry;
                    if (ModTek.Config.EnableDebugLogging)
                        RLog.M.WL(1, "addendumsManifest[" + addendum.Name + "]["+resourceType+"]["+entry.Id+"] " + versionManifestEntry.FilePath);
                }
            }
            sw.Stop();
            //RLog.M.WL(0, $"BTRL ADD took: {sw.ElapsedMilliseconds}ms");

            sw.Restart();
            foreach (var entry in ModTek.RemoveBTRLEntries)
            {
                var resourceType = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), entry.Type);
                // entry.RemoveFromModTekManifest(resourceType); THIS WAS A NOP

                if (___baseManifest.ContainsKey(resourceType) && ___baseManifest[resourceType].ContainsKey(entry.Id))
                {
                    ___baseManifest[resourceType].Remove(entry.Id);
                    RLog.M.WL(1, "baseManifest remove:" + resourceType + " " + entry.Id);
                }

                if (___contentPacksManifest.ContainsKey(resourceType) && ___contentPacksManifest[resourceType].ContainsKey(entry.Id))
                {
                    ___contentPacksManifest[resourceType].Remove(entry.Id);
                    RLog.M.WL(1, "contentPacksManifest remove:" + resourceType + " " + entry.Id);
                }

                var containingAddendums = ___addendumsManifest.Where(pair => pair.Value.ContainsKey(resourceType) && pair.Value[resourceType].ContainsKey(entry.Id));
                foreach (var containingAddendum in containingAddendums)
                {
                    RLog.M.WL(1, "remove addendumsManifest[" + resourceType + "][" + entry.Id + "]");
                    containingAddendum.Value[resourceType].Remove(entry.Id);
                }
            }
            sw.Stop();

            __state.Stop();

            if (ModTek.Config.EnableDebugLogging)
                RLog.M.TWL(0, $"BattleTechResourceLocator.RefreshTypedEntries complete in {__state.ElapsedMilliseconds}ms");
        }
    }
}
