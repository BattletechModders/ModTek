using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using static ModTek.Util.Logger;

namespace ModTek.Util
{
    public static class JSONMerger
    {
        private static bool IsAdvancedJSONMerge(JObject merge)
        {
            return (merge[nameof(AdvancedJSONMerge.TargetID)] != null || merge[nameof(AdvancedJSONMerge.TargetIDs)] != null)
                && merge[nameof(AdvancedJSONMerge.Instructions)] != null;
        }

        private static void DoAdvancedMerge(JObject target, JObject merge)
        {
            var instructions = merge[nameof(AdvancedJSONMerge.Instructions)].ToObject<List<AdvancedJSONMerge.Instruction>>();
            foreach (var instruction in instructions)
            {
                if (!instruction.Process(target))
                    Log($"Warning: An instruction (Action: '{instruction.Action}' JSONPath: '{instruction.JSONPath}') did not perform anything.");
            }
        }

        public static void MergeIntoTarget(JObject target, JObject merge)
        {
            if (IsAdvancedJSONMerge(merge))
            {
                DoAdvancedMerge(target, merge);
                return;
            }

            target.Merge(merge, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
        }
    }

    public class AdvancedJSONMerge
    {
        public enum MergeAction
        {
            ArrayAdd, // adds a given value to the end of the target array
            ArrayAddAfter, // adds a given value after the target element in the array
            ArrayAddBefore, // adds a given value before the target element in the array
            ArrayConcat, // adds a given array to the end of the target array
            ObjectMerge, // merges a given object with the target objects
            Remove, // removes the target element(s)
            Replace // replaces the target with a given value
        }

        public class Instruction
        {
            [JsonProperty(Required = Required.Always)]
            [JsonConverter(typeof(StringEnumConverter))]
            public MergeAction Action;

            [JsonProperty(Required = Required.Always)]
            public string JSONPath;

            public JToken Value;

            public bool Process(JObject root)
            {
                var tokens = root.SelectTokens(JSONPath).ToList();

                if (tokens.Count == 0)
                    return false;

                if (Action == MergeAction.Remove)
                {
                    foreach (var jToken in tokens)
                    {
                        if (jToken.Parent is JProperty)
                            jToken.Parent.Remove();
                        else
                            jToken.Remove();
                    }

                    return true;
                }

                if (tokens.Count > 1)
                    throw new Exception("JSONPath can't point to more than one token outside of the Remove action");

                var token = tokens[0];
                switch (Action)
                {
                    case MergeAction.Replace:
                        token.Replace(Value);
                        break;
                    case MergeAction.ArrayAdd:
                    {
                        if (!(token is JArray a))
                            throw new Exception("JSONPath needs to point an array");

                        a.Add(Value);
                        break;
                    }
                    case MergeAction.ArrayAddAfter:
                        token.AddAfterSelf(Value);
                        break;
                    case MergeAction.ArrayAddBefore:
                        token.AddBeforeSelf(Value);
                        break;
                    case MergeAction.ObjectMerge:
                    {
                        if (!(token is JObject o1) || !(Value is JObject o2))
                            throw new Exception("JSONPath has to point to an object and Value has to be an object");

                        // same behavior as partial json merging
                        o1.Merge(o2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                        break;
                    }
                    case MergeAction.ArrayConcat:
                    {
                        if (!(token is JArray a1) || !(Value is JArray a2))
                            throw new Exception("JSONPath has to point to an array and Value has to be an array");

                        a1.Merge(a2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                        break;
                    }
                    default:
                        throw new Exception("Unhandled action in Process");
                }

                return true;
            }
        }

        public string TargetID;
        public List<string> TargetIDs;
        public string TargetType;

        [JsonProperty(Required = Required.Always)]
        public List<Instruction> Instructions;

        public static AdvancedJSONMerge FromFile(string path)
        {
            if (!File.Exists(path))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<AdvancedJSONMerge>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                LogException($"\tCould not read AdvancedJSONMerge in path: {path}", e);
                return null;
            }
        }
    }
}
