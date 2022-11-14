using System;
using System.Collections.Generic;
using System.IO;
using BattleTech;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Features.CustomDebugSettings.Patches
{
    /// <summary>
    /// Patch the GameTipList to use modded tip list if existing.
    /// </summary>
    [HarmonyPatch(typeof(DebugSettings), nameof(DebugSettings.Load))]
    internal static class DebugSettings_Load_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(
                    AccessTools.Method(typeof(TextReader), nameof(TextReader.ReadToEnd)),
                    AccessTools.Method(typeof(DebugSettings_Load_Patch), nameof(ReadToEnd))
                );
        }

        public static string ReadToEnd(this StreamReader reader)
        {
            try
            {
                return DebugSettingsFeature.GetDebugSettings();
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log("Failed trying to read custom debug settings", e);
                return reader.ReadToEnd();
            }
        }
    }
}
