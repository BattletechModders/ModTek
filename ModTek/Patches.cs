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
        public static HashSet<Int32> JSONHashes = new HashSet<Int32>();
        public static Dictionary<string, List<string>> JSONMerges = new Dictionary<string, List<string>>();

        public static void Execute(ref string json)
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
    
    [HarmonyPatch(typeof(HBS.Util.JSONSerializationUtility), "StripHBSCommentsFromJSON")]
    public static class HBS_Util_JSONSerializationUtility_StripHBSCommentsFromJSON_Patch
    {
        public static void Postfix(string json, ref string __result)
        {
            // function has invalid json coming from file
            // and hopefully valid json (i.e. comments out) coming out from function
            if (DoJSONMerge.JSONHashes.Contains(json.GetHashCode()))
            {
                ModTek.LogWithDate("Hash hit so running JSON Merge");
                DoJSONMerge.Execute(ref __result);
            }
        }
    }

    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        static void Postfix(VersionManifest __result)
        {
            // add to the manifest here
            // TODO: these freaking kvp look so bad
            foreach (var entryKVP in ModTek.NewManifestEntries)
            {
                var id = entryKVP.Key;
                var newEntry = entryKVP.Value;
                
                if (newEntry.ShouldMergeJSON && __result.Contains(id, newEntry.Type))
                {
                    // read the manifest pointed entry and hash the contents
                    DoJSONMerge.JSONHashes.Add(File.ReadAllText(__result.Get(id, newEntry.Type).FilePath).GetHashCode());

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
