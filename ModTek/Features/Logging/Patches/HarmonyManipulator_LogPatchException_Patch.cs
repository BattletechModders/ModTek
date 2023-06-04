using System;
using HarmonyLib.Public.Patching;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(HarmonyManipulator), "LogPatchException")]
internal static class HarmonyManipulator_LogPatchException_Patch
{
    [HarmonyPrefix]
    public static void Prefix(ref bool __runOriginal, object errorObject, string patch)
    {
        if (errorObject is Exception e)
        {
            Log.HarmonyX.Error?.Log($"HarmonySafeWrap: Error running patch {patch}", e);
        }
        else
        {
            Log.HarmonyX.Error?.Log("HarmonySafeWrap: Error running patch {patch}: " + errorObject);
        }
        __runOriginal = false;
    }
}