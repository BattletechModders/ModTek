using System;
using Harmony;
using Harmony.ILCopying;
using ModTek.Features.Logging;

namespace ModTek.Features.HarmonyPatching.Patches
{
    [HarmonyPatch(typeof(Memory), nameof(Memory.GetMethodStart))]
    internal class Memory_GetMethodStart_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(Exception exception)
        {
            try
            {
                if (exception != null)
                {
                    MTLogger.Info.Log("Full exception from failed patch", exception);
                }
            }
            catch (Exception e)
            {
                MTLogger.Error.Log("Failed running prefix", e);
            }
        }
    }
}
