using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using Harmony;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch]
    public static class DataManagerFileLoadRequest_OnLoadedWithText_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static IEnumerable<MethodBase> GetOnLoadedWithTextMethods()
        {
            // using same hook as CAC/CustomLocalization to avoid being ignored
            yield return AccessTools.Method(typeof(DataManager.StringDataLoadRequest<WeaponDef>), "OnLoadedWithText");
            yield return AccessTools.Method(typeof(DataManager.CSVDataLoadRequest<CSVReader>), "OnLoadedWithText");
        }

        public static IEnumerable<MethodBase> GetLoadMethods()
        {
            yield return AccessTools.Method(typeof(DataManager.StringDataLoadRequest<WeaponDef>), "Load");
            yield return AccessTools.Method(typeof(DataManager.CSVDataLoadRequest<CSVReader>), "Load");
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            return GetOnLoadedWithTextMethods();
        }

        [HarmonyPriority(Priority.High)]
        public static void Prefix(VersionManifestEntry ___manifestEntry, ref string text)
        {
            ModsManifest.MergeContentIfApplicable(___manifestEntry, ref text);
        }
    }
}
