using System;
using System.Collections.Generic;
using System.IO;
using BattleTech;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Features.CustomGameTips.Patches
{
    /// <summary>
    /// Patch the GameTipList to use modded tip list if existing.
    /// </summary>
    [HarmonyPatch(typeof(GameTipList), MethodType.Constructor, typeof(string), typeof(int))]
    internal static class GameTipList_ctor_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(
                    AccessTools.Method(typeof(File), nameof(File.ReadAllText), new []{typeof(string)}),
                    AccessTools.Method(typeof(GameTipList_ctor_Patch), nameof(ReadAllText))
                );
        }

        public static string ReadAllText(string path)
        {
            try
            {
                return GameTipsFeature.GetGameTip(path);
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log("Failed trying to read custom GameTip", e);
                return File.ReadAllText(path);
            }
        }
    }
}
