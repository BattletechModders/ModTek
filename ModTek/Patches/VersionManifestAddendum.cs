using BattleTech;
using Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using ModTek.Logging;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch the LoadDefaultManifest to use the cached manifest that is built at ModTek load instead of rebuilding it
    /// This is primarily a performance optimization
    /// </summary>
    [HarmonyPatch(typeof(VersionManifest), "GetAddendumContainingEntry")]
    [HarmonyPatch(
        new Type[]
        {
            typeof(string),
            typeof(string)
        }
    )]
    internal static class VersionManifest_GetAddendumContainingEntry
    {
        private static bool Prepare()
        {
            return ModTek.Enabled;
        }

        private static bool Prefix(string id, string type, ref VersionManifestAddendum __result, List<VersionManifestAddendum> ___addendums)
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
    internal static class VersionManifest_GetAddendumContainingEntry_VME
    {
        // Necessary as VersionManifestEntry is out - ergo a ref, and can't be defined in an attribute
        private static MethodBase TargetMethod(HarmonyInstance instance)
        {
            var type = AccessTools.Method(
                typeof(VersionManifest),
                "GetAddendumContainingEntry",
                new Type[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(VersionManifestEntry).MakeByRefType()
                }
            );
            return type;
        }

        private static bool Prepare()
        {
            return ModTek.Enabled;
        }

        private static bool Prefix
        (
            string id,
            string type,
            out VersionManifestEntry entry,
            ref VersionManifestAddendum __result,
            List<VersionManifestAddendum> ___addendums
        )
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

    internal static class VersionManifestAddendumCache
    {
        private static Dictionary<string, VersionManifestAddendum> lookupCache = new();

        private static Dictionary<string, int> cachedAddendums = new();

        private static void UpdateCache(List<VersionManifestAddendum> addendums)
        {
            foreach (var vma in addendums)
            {
                if (!cachedAddendums.ContainsKey(vma.Name))
                {
                    //RLog.LogWrite($"Adding to addendum cache: {vma.Name}\n");
                    foreach (var vme in vma.Entries)
                    {
                        var entryKey = $"{vme.Id}_{vme.Type}";
                        if (!lookupCache.ContainsKey(entryKey))
                        {
                            lookupCache.Add(entryKey, vma);
                        }
                    }

                    cachedAddendums.Add(vma.Name, 0);
                }
            }
        }

        public static bool FindInCache(string id, string type, out VersionManifestAddendum addendum)
        {
            var key = $"{id}_{type}";
            var existsInCache = lookupCache.TryGetValue(key, out addendum);
            if (!existsInCache)
            {
                addendum = null;
            }

            return existsInCache;
        }

        public static VersionManifestAddendum FindInAddendums(string id, string type, List<VersionManifestAddendum> addendums)
        {
            UpdateCache(addendums);
            var wasFound = FindInCache(id, type, out var addendum);
            return wasFound ? addendum : null;
        }

        public static void InvalidateAddendum(VersionManifestAddendum addendum)
        {
            if (lookupCache.ContainsKey(addendum.Name))
            {
                RLog.M.WL(0, "Invalidating cache for addendum: " + addendum.Name);
                lookupCache.Remove(addendum.Name);
            }
        }
    }
}
