using System;
using System.Diagnostics;
using BattleTech.Data;
using Harmony;
using ModTek.Features.Manifest.BTRL;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(ContentPackIndex), "PatchMDD")]
    public static class ContentPackIndex_PatchMDD_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Prefix(out bool ___rebuildMDDOnLoadComplete, out Stopwatch __state)
        {
            __state = new Stopwatch();
            __state.Start();

            ___rebuildMDDOnLoadComplete = true; //rebuilding is less work than having to track changes
        }

        public static void Postfix(ContentPackIndex __instance, Stopwatch __state)
        {
            try
            {
                BetterBTRL.Instance.PackIndex.PatchMDD(__instance);
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log("Error running postfix", e);
            }

            Log.Main.Debug?.LogIfSlow(__state, "ContentPackIndex.PatchMDD");
        }
    }
}
