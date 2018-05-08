using Harmony;
using System;
using System.Diagnostics.CodeAnalysis;
using BattleTech;
using JetBrains.Annotations;

namespace ModTek
{
    using static Logger;

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(VersionInfo), "GetReleaseVersion")]
    public static class VersionInfo_GetReleaseVersion_Patch
    {
        [UsedImplicitly]
        public static void Postfix(ref string __result)
        {
            string old = __result;
            __result = old + " w/ ModTek";
        }
    }

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        [UsedImplicitly]
        public static void Postfix(VersionManifest __result)
        {
            // add to the manifest here
            // TODO: these freaking kvp look so bad
            foreach (var entryKVP in ModTek.NewManifestEntries)
            {
                var id = entryKVP.Key;
                var path = entryKVP.Value.Path;
                var type = entryKVP.Value.Type;

                Log($"\tAddOrUpdate({id},{path},{type},{DateTime.Now})");
                __result.AddOrUpdate(id, path, type, DateTime.Now);
            }
        }
    }
}
