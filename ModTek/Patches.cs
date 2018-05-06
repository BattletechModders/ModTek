using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModTek
{
    [HarmonyPatch(typeof(VersionInfo), "GetReleaseVersion")]
    public static class VersionInfo_GetReleaseVersion_Patch
    {
        static void Postfix(ref string __result)
        {
            string old = __result;
            __result = old + " w/ ModTek";
        }
    }

    [HarmonyPatch(typeof(BattleTech.VersionManifestUtilities), "LoadDefaultManifest")]
    public static class BattleTech_VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        static void Postfix(BattleTech.VersionManifest __result)
        {
            ModTek.LogWithDate("BattleTech_VersionManifestUtilities_LoadDefaultManifest_Patch");

            // add to the manifest here
            // TODO: these freaking kvp look so bad
            foreach (var entryKVP in ModTek.NewManifestEntries)
            {
                var id = entryKVP.Key;
                var path = entryKVP.Value.Path;
                var type = entryKVP.Value.Type;

                ModTek.Log("\tAddOrUpdate({0},{1},{2},{3})", entryKVP.Key, path, type, DateTime.Now);
                __result.AddOrUpdate(entryKVP.Key, path, type, DateTime.Now);
            }
        }
    }
}
