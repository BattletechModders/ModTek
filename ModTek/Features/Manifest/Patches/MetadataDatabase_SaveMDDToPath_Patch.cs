using System;
using BattleTech.Data;
using Harmony;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch(typeof(MetadataDatabase), nameof(MetadataDatabase.SaveMDDToPath))]
    internal static class MetadataDatabase_SaveMDDToPath_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(out Guid __state)
        {
            __state = Guid.NewGuid();
            Log($"{__state} MetadataDatabase.SaveMDDToPath was called by:\n{Environment.StackTrace}");
        }

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref Guid __state)
        {
            Log($"{__state} MetadataDatabase.SaveMDDToPath finished being called.");
        }
    }
}
