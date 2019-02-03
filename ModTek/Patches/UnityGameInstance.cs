using System.Collections.Generic;
using System.Reflection.Emit;
using BattleTech;
using Harmony;
using ModTek.Util;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch the ShutdownGame method to remove the MetadataDatabase.Instance.Shutdown()
    /// </summary>
    [HarmonyPatch(typeof(UnityGameInstance), "ShutdownGame")]
    public static class UnityGameInstance_ShutdownGame_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            if (VersionInfo.ProductVersion != "1.4.0")
            {
                Logger.Log("NOT PATCHING UnityGameInstance.ShutdownGame since not 1.4.0");
                return code;
            }

            for (var index = 0; index < code.Count; index++)
            {
                if (code[index].opcode != OpCodes.Call || !code[index].operand.ToString().Contains("MetadataDatabase"))
                    continue;

                code[index].opcode = OpCodes.Nop;
                code[index + 1].opcode = OpCodes.Nop;
                break;
            }

            return code;
        }
    }
}
