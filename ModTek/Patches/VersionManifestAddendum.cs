using BattleTech;
using Harmony;
using ModTek.RuntimeLog;
using System;
using System.Collections.Generic;
using System.Reflection;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch the LoadDefaultManifest to use the cached manifest that is built at ModTek load instead of rebuilding it
    /// This is primarily a performance optimization
    /// </summary>
    [HarmonyPatch(typeof(VersionManifest), "GetAddendumContainingEntry")]
    [HarmonyPatch(new Type[] { typeof(string), typeof(string) })]
    static class VersionManifest_GetAddendumContainingEntry
    {

        static bool Prepare() => ModTek.Enabled;

        static bool Prefix(string id, string type, ref VersionManifestAddendum __result, List<VersionManifestAddendum> ___addendums)
        {
            VersionManifestAddendum queryValue;
            if (!VersionManifestAddendumCache.FindInCache(id, type, out queryValue))
            {
                queryValue = VersionManifestAddendumCache.FindInAddendums(id, type, ___addendums);
            }

            __result = queryValue;
            return false;
        }
    }

    [HarmonyPatch(typeof(VersionManifest), "GetAddendumContainingEntry")]
    static class VersionManifest_GetAddendumContainingEntry_VME
    {

        // Necessary as VersionManifestEntry is out - ergo a ref, and can't be defined in an attribute
        static MethodBase TargetMethod(HarmonyInstance instance)
        {
            MethodInfo type = AccessTools.Method(typeof(VersionManifest), "GetAddendumContainingEntry",
                new Type[] { typeof(string), typeof(string), typeof(VersionManifestEntry).MakeByRefType() });
            return type;
        }

        static bool Prepare() => ModTek.Enabled;

        static bool Prefix(string id, string type, out VersionManifestEntry entry, ref VersionManifestAddendum __result,
            List<VersionManifestAddendum> ___addendums)
        {
            VersionManifestAddendum queryValue;
            if (!VersionManifestAddendumCache.FindInCache(id, type, out queryValue))
            {
                queryValue = VersionManifestAddendumCache.FindInAddendums(id, type, ___addendums);
            }

            entry = queryValue.Get(id, type);
            __result = queryValue;
            return false;
        }
    }

    static class VersionManifestAddendumCache
    {
        private static Dictionary<string, VersionManifestAddendum> lookupCache = new Dictionary<string, VersionManifestAddendum>();

        private static Dictionary<string, int> cachedAddendums = new Dictionary<string, int>();

        private static void UpdateCache(List<VersionManifestAddendum> addendums)
        {
            foreach (VersionManifestAddendum vma in addendums)
            {
                if (!cachedAddendums.ContainsKey(vma.Name))
                {
                    RLog.LogWrite($"Adding to addendum cache: {vma.Name}\n");
                    foreach (VersionManifestEntry vme in vma.Entries)
                    {
                        string entryKey = $"{vme.Id}_{vme.Type}";
                        if (!lookupCache.ContainsKey(entryKey))
                            lookupCache.Add(entryKey, vma);
                    }

                    cachedAddendums.Add(vma.Name, 0);
                }
            }
        }

        public static bool FindInCache(string id, string type, out VersionManifestAddendum addendum)
        {
            string key = $"{id}_{type}";
            bool existsInCache = lookupCache.TryGetValue(key, out addendum);
            if (!existsInCache)
                addendum = null;

            return existsInCache;
        }

        public static VersionManifestAddendum FindInAddendums(string id, string type, List<VersionManifestAddendum> addendums)
        {
            UpdateCache(addendums);
            bool wasFound = FindInCache(id, type, out VersionManifestAddendum addendum);       
            return addendum;
        }

    }
}
