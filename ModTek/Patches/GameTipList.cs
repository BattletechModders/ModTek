using System.Collections.Generic;
using System.IO;
using BattleTech;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    [HarmonyPatch(typeof(GameTipList), MethodType.Constructor, typeof(string), typeof(int))]
    public static class GameTipList_ctor_Patch
    {
        public static void Postfix(string filename, List<string> ___tips)
        {
            if (!ModTek.ModGameTips.ContainsKey(filename))
                return;

            var text = File.ReadAllText(ModTek.ModGameTips[filename]);
            var textSplit = text.Split('\n');

            ___tips.Clear();

            foreach (var tip in textSplit)
            {
                var trimmedTip = tip.Trim();
                if (!string.IsNullOrEmpty(trimmedTip))
                    ___tips.Add(trimmedTip);
            }
        }
    }
}
