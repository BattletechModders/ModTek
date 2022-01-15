using System;
using System.Collections.Generic;
using BattleTech.Data;
using Harmony;
using ModTek.Features.Logging;
using ModTek.Features.Manifest.BTRL;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(ContentPackIndex), "TryFinalizeDataLoad")]
    public static class ContentPackIndex_TryFinalizeDataLoad_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        [HarmonyPriority(Priority.High)]
        public static void Prefix(ContentPackIndex __instance, Dictionary<string, string> ___resourceMap)
        {
            try
            {
                BetterBTRL.Instance.PackIndex.TryFinalizeDataLoad(__instance, ___resourceMap);
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
        }

        [HarmonyPriority(Priority.Low)]
        public static void Postfix(ContentPackIndex __instance)
        {
            try
            {
                if (__instance.AllContentPacksLoaded())
                {
                    BetterBTRL.Instance.ContentPackManifestsLoaded();
                }
            }
            catch (Exception e)
            {
                MTLogger.Info.Log("Error running prefix", e);
            }
        }
    }
}
