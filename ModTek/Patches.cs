using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace ModTek
{
    [HarmonyPatch(typeof(VersionInfo), "GetReleaseVersion")]
    public static class VersionInfo_GetReleaseVersion_Patch
    {
        static void Postfix(ref string __result)
        {
            string old = __result;
            __result = old + " w/ ModTek";
        }
    }

    public static class DoModulePatch
    {
        public static Dictionary<string, List<string>> ModulePatches = new Dictionary<string, List<string>>();
        public static void Execute<T>(ref string json, T __instance)
        {
            var copy_json = json;
            try
            {
                var oldJObj = Newtonsoft.Json.Linq.JObject.Parse(copy_json);
                var id = ModTek.InferIDFromJSONBlob(oldJObj);
                if (ModulePatches.ContainsKey(id))
                {
                    ModulePatches[id].ForEach((string json_patch) =>
                    {
                        var patchJObj = Newtonsoft.Json.Linq.JObject.Parse(json_patch);
                        oldJObj.Merge(patchJObj);
                    });
                    ModulePatches.Remove(id);
                }
                // Once we are here, we can commit to changing the json file
                json = oldJObj.ToString();
            }
            catch (JsonReaderException e)
            {
                ModTek.Log(e.Message);
                ModTek.Log(e.StackTrace);
                ModTek.Log("Error encountered loading json, skipping any patches which would have been applied.");
            }
        }
    }
    
    [HarmonyPatch(typeof(BattleTech.AbilityDef), "FromJSON")]
    public static class BattleTech_AbilityDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.AbilityDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.AmmunitionBoxDef), "FromJSON")]
    public static class BattleTech_AmmunitionBoxDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.AmmunitionBoxDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.AmmunitionDef), "FromJSON")]
    public static class BattleTech_AmmoDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.AmmunitionDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.AudioEventDef), "FromJSON")]
    public static class BattleTech_AudioEventDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.AudioEventDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.BackgroundDef), "FromJSON")]
    public static class BattleTech_BackgroundDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.BackgroundDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.BuildingDef), "FromJSON")]
    public static class BattleTech_BuildingDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.BuildingDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.CastDef), "FromJSON")]
    public static class BattleTech_CastDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.CastDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.ChassisDef), "FromJSON")]
    public static class BattleTech_ChassisDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.ChassisDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.HardpointDataDef), "FromJSON")]
    public static class BattleTech_HardpointDataDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.HardpointDataDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.HeatSinkDef), "FromJSON")]
    public static class BattleTech_HeatSinkDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.HeatSinkDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.JumpJetDef), "FromJSON")]
    public static class BattleTech_JumpJetDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.JumpJetDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.LanceDef), "FromJSON")]
    public static class BattleTech_LanceDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.LanceDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.MechDef), "FromJSON")]
    public static class BattleTech_MechDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.MechDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.PilotDef), "FromJSON")]
    public static class BattleTech_PilotDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.PilotDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.TurretDef), "FromJSON")]
    public static class BattleTech_TurretDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.TurretDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.TurretChassisDef), "FromJSON")]
    public static class BattleTech_TurretChassisDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.TurretChassisDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.VehicleDef), "FromJSON")]
    public static class BattleTech_VehicleDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.VehicleDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.WeaponDef), "FromJSON")]
    public static class BattleTech_WeaponDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.WeaponDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.UpgradeDef), "FromJSON")]
    public static class BattleTech_UpgradeDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BattleTech.UpgradeDef __instance)
        {
            DoModulePatch.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BattleTech.VersionManifestUtilities), "LoadDefaultManifest")]
    public static class BattleTech_VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        static void Postfix(BattleTech.VersionManifest __result)
        {
            ModTek.LogWithDate("BattleTech_VersionManifestUtilities_LoadDefaultManifest_Patch");

            // add to the manifest here
            // TODO: these freaking kvp look so bad
            foreach (var entryKVP in ModTek.NewManifestEntries)
            {
                var id = entryKVP.Key;
                var path = entryKVP.Value.Path;
                var type = entryKVP.Value.Type;
                
                ModTek.Log("\tAddOrUpdate({0},{1},{2},{3})", entryKVP.Key, path, type, DateTime.Now);

                if (__result.Contains(id, type))
                {
                    // The registry already contains this information, so we need to throw it through a json merger.
                    var json = File.ReadAllText(path);

                    if(!DoModulePatch.ModulePatches.ContainsKey(id))
                    {
                        DoModulePatch.ModulePatches.Add(id, new List<string>());
                    }

                    DoModulePatch.ModulePatches[id].Add(json);
                }
                else
                {
                    // This is a new definition, so it can be added directly to the registry.
                    __result.AddOrUpdate(entryKVP.Key, path, type, DateTime.Now);
                }
            }
        }
    }
}
