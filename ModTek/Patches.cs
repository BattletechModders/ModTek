using Harmony;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var old = __result;
            __result = old + " w/ ModTek";
        }
    }
    
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(HBS.Util.JSONSerializationUtility), "StripHBSCommentsFromJSON")]
    public static class HBS_Util_JSONSerializationUtility_StripHBSCommentsFromJSON_Patch
    {
        public static void Postfix(string json, ref string __result)
        {
            // function has invalid json coming from file
            // and hopefully valid json (i.e. comments out) coming out from function
            ModTek.TryMergeJsonInto(json, ref __result);
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
            ModTek.TryAddToVersionManifest(__result);
        }
    }
}
