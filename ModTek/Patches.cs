using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using Harmony;
using HBS.Util;
using JetBrains.Annotations;

namespace ModTek
{
    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(VersionInfo), "GetReleaseVersion")]
    public static class VersionInfo_GetReleaseVersion_Patch
    {
        [UsedImplicitly]
        public static void Postfix(ref string __result)
        {
            var old = __result;
            __result = old + $" w/ ModTek v{Assembly.GetExecutingAssembly().GetName().Version}";
        }
    }

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(JSONSerializationUtility), "StripHBSCommentsFromJSON")]
    public static class JSONSerializationUtility_StripHBSCommentsFromJSON_Patch
    {
        [UsedImplicitly]
        public static void Postfix(string json, ref string __result)
        {
            // function has invalid json coming from file
            // and hopefully valid json (i.e. comments out) coming out from function
            ModTek.TryMergeIntoInterceptedJson(json, ref __result);
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

    [UsedImplicitly]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [HarmonyPatch(typeof(DataManager), new[] { typeof(MessageCenter) })]
    public static class DataManager_CTOR_Patch
    {
        [UsedImplicitly]
        public static void Prefix()
        {
            ModTek.LoadMods();
        }
    }
}
