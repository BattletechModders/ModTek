using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BattleTech;

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

    public static class DoJSONMerge
    {
        public static Dictionary<string, List<string>> JSONMerges = new Dictionary<string, List<string>>();
        public static void Execute<T>(ref string json, T __instance)
        {
            var copy_json = json;
            try
            {
                var oldJObj = JObject.Parse(copy_json);
                var id = ModTek.InferIDFromJObject(oldJObj);
                if (JSONMerges.ContainsKey(id))
                {
                    JSONMerges[id].ForEach((string json_patch) =>
                    {
                        var patchJObj = JObject.Parse(json_patch);
                        oldJObj.Merge(patchJObj);
                    });
                    JSONMerges.Remove(id);
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
    
    [HarmonyPatch(typeof(AbilityDef), "FromJSON")]
    public static class AbilityDef_FromJSON_Patch
    {
        static void Prefix(ref string json, AbilityDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(AmmunitionBoxDef), "FromJSON")]
    public static class AmmunitionBoxDef_FromJSON_Patch
    {
        static void Prefix(ref string json, AmmunitionBoxDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(AmmunitionDef), "FromJSON")]
    public static class AmmoDef_FromJSON_Patch
    {
        static void Prefix(ref string json, AmmunitionDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(AudioEventDef), "FromJSON")]
    public static class AudioEventDef_FromJSON_Patch
    {
        static void Prefix(ref string json, AudioEventDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BackgroundDef), "FromJSON")]
    public static class BackgroundDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BackgroundDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(BuildingDef), "FromJSON")]
    public static class BuildingDef_FromJSON_Patch
    {
        static void Prefix(ref string json, BuildingDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(CastDef), "FromJSON")]
    public static class CastDef_FromJSON_Patch
    {
        static void Prefix(ref string json, CastDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(ChassisDef), "FromJSON")]
    public static class ChassisDef_FromJSON_Patch
    {
        static void Prefix(ref string json, ChassisDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(HardpointDataDef), "FromJSON")]
    public static class HardpointDataDef_FromJSON_Patch
    {
        static void Prefix(ref string json, HardpointDataDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(HeatSinkDef), "FromJSON")]
    public static class HeatSinkDef_FromJSON_Patch
    {
        static void Prefix(ref string json, HeatSinkDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(JumpJetDef), "FromJSON")]
    public static class JumpJetDef_FromJSON_Patch
    {
        static void Prefix(ref string json, JumpJetDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(LanceDef), "FromJSON")]
    public static class LanceDef_FromJSON_Patch
    {
        static void Prefix(ref string json, LanceDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(MechDef), "FromJSON")]
    public static class MechDef_FromJSON_Patch
    {
        static void Prefix(ref string json, MechDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(PilotDef), "FromJSON")]
    public static class PilotDef_FromJSON_Patch
    {
        static void Prefix(ref string json, PilotDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(TurretDef), "FromJSON")]
    public static class TurretDef_FromJSON_Patch
    {
        static void Prefix(ref string json, TurretDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(TurretChassisDef), "FromJSON")]
    public static class TurretChassisDef_FromJSON_Patch
    {
        static void Prefix(ref string json, TurretChassisDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(VehicleDef), "FromJSON")]
    public static class VehicleDef_FromJSON_Patch
    {
        static void Prefix(ref string json, VehicleDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(WeaponDef), "FromJSON")]
    public static class WeaponDef_FromJSON_Patch
    {
        static void Prefix(ref string json, WeaponDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(UpgradeDef), "FromJSON")]
    public static class UpgradeDef_FromJSON_Patch
    {
        static void Prefix(ref string json, UpgradeDef __instance)
        {
            DoJSONMerge.Execute(ref json, __instance);
        }
    }

    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        static void Postfix(VersionManifest __result)
        {
            ModTek.LogWithDate("VersionManifestUtilities_LoadDefaultManifest_Patch");

            // add to the manifest here
            // TODO: these freaking kvp look so bad
            foreach (var entryKVP in ModTek.NewManifestEntries)
            {
                var id = entryKVP.Key;
                var newEntry = entryKVP.Value;
                
                if (newEntry.ShouldMergeJSON && __result.Contains(id, newEntry.Type))
                {
                    // The manifest already contains this information, so we need to queue it to be merged
                    var partialJSON = File.ReadAllText(newEntry.Path);

                    if(!DoJSONMerge.JSONMerges.ContainsKey(id))
                    {
                        DoJSONMerge.JSONMerges.Add(id, new List<string>());
                    }

                    ModTek.Log("\tAdding id {0} to JSONMerges", id);
                    DoJSONMerge.JSONMerges[id].Add(partialJSON);
                }
                else
                {
                    // This is a new definition or a replacement that doesn't get merged, so add or update the manifest
                    ModTek.Log("\tAddOrUpdate({0}, {1}, {2}, {3}, {4}, {5})", id, newEntry.Path, newEntry.Type, DateTime.Now, newEntry.AssetBundleName, newEntry.AssetBundlePersistent);
                    __result.AddOrUpdate(id, newEntry.Path, newEntry.Type, DateTime.Now, newEntry.AssetBundleName, newEntry.AssetBundlePersistent);
                }
            }
        }
    }
}
