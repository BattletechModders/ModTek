using Harmony;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Disable activateAfterInit from functioning for the Start() on the "Main" game object which activates the BattleTechGame object
    /// This stops the main game object from loading immediately -- so work can be done beforehand
    /// </summary>
    [HarmonyPatch(typeof(ActivateAfterInit), "Start")]
    public static class ActivateAfterInit_Start_Patch
    {
        public static bool Prefix(ActivateAfterInit __instance)
        {
            var traverse = Traverse.Create(__instance);
            if (ActivateAfterInit.ActivateAfter.Start.Equals(traverse.Field("activateAfter").GetValue<ActivateAfterInit.ActivateAfter>()))
            {
                var gameObjects = traverse.Field("activationSet").GetValue<GameObject[]>();
                foreach (var gameObject in gameObjects)
                {
                    if ("BattleTechGame".Equals(gameObject.name))
                    {
                        // Don't activate through this call!
                        return false;
                    }
                }
            }
            // Call the method
            return true;
        }
    }
}
