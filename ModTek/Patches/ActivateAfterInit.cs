using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches;

/// <summary>
/// Disable activateAfterInit from functioning for the Start() on the "Main" game object which activates the BattleTechGame object
/// This stops the main game object from loading immediately -- so work can be done beforehand
/// </summary>
[HarmonyPatch(typeof(ActivateAfterInit), "Start")]
internal static class ActivateAfterInit_Start_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static bool Prefix(ActivateAfterInit __instance, ActivateAfterInit.ActivateAfter ___activateAfter, GameObject[] ___activationSet)
    {
        //Log("ActivateAfterInit.Start activateAfter:" + ___activateAfter);
        //foreach(GameObject gameObject in ___activationSet)
        //{
        //Log("\t"+ gameObject.name);
        //}
        if (ActivateAfterInit.ActivateAfter.Start.Equals(__instance.activateAfter))
        {
            var gameObjects = __instance.activationSet;
            foreach (var gameObject in gameObjects)
            {
                if ("SplashLauncher".Equals(gameObject.name))
                {
                    // Don't activate through this call!
                    return false;
                }
            }
        }

        return true;
        // Call the method
        //return true;
    }
}