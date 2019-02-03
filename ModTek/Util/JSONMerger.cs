using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace ModTek.Util
{
    public static class JSONMerger
    {
        public enum AdvancedMergeAction
        {
            ArrayAdd, // adds a given value to the end of the target array
            ArrayAddAfter, // adds a given value after the target element in the array
            ArrayAddBefore, // adds a given value before the target element in the array
            ArrayConcat, // adds a given array to the end of the target array
            ObjectMerge, // merges a given object with the target objects
            Remove, // removes the target element(s)
            Replace // replaces the target with a given value
        }

        public class AdvancedMergeInstruction
        {
            [JsonProperty(Required = Required.Always)] [JsonConverter(typeof(StringEnumConverter))]
            public AdvancedMergeAction Action;

            [JsonProperty(Required = Required.Always)]
            public string JSONPath;

            // TODO add external JSON support
            public JToken Value;

            public void Process(JObject root)
            {
                var tokens = root.SelectTokens(JSONPath).ToList();

                if (tokens.Count == 0)
                    throw new Exception("JSONPath does not point to anything");

                if (Action == AdvancedMergeAction.Remove)
                {
                    foreach (var jToken in tokens)
                    {
                        if (jToken.Parent is JProperty)
                            jToken.Parent.Remove();
                        else
                            jToken.Remove();
                    }

                    return;
                }

                if (tokens.Count > 1)
                    throw new Exception("JSONPath can't point to more than one token outside of the Remove action");

                var token = tokens[0];
                switch (Action)
                {
                    case AdvancedMergeAction.Replace:
                        token.Replace(Value);
                        break;
                    case AdvancedMergeAction.ArrayAdd:
                    {
                        if (!(token is JArray a))
                            throw new Exception("JSONPath needs to point an array");

                        a.Add(Value);
                        break;
                    }
                    case AdvancedMergeAction.ArrayAddAfter:
                        token.AddAfterSelf(Value);
                        break;
                    case AdvancedMergeAction.ArrayAddBefore:
                        token.AddBeforeSelf(Value);
                        break;
                    case AdvancedMergeAction.ObjectMerge:
                    {
                        if (!(token is JObject o1) || !(Value is JObject o2))
                            throw new Exception("JSONPath has to point to an object and Value has to be an object");

                        // same behavior as partial json merging
                        o1.Merge(o2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                        break;
                    }
                    case AdvancedMergeAction.ArrayConcat:
                    {
                        if (!(token is JArray a1) || !(Value is JArray a2))
                            throw new Exception("JSONPath has to point to an array and Value has to be an array");

                        a1.Merge(a2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                        break;
                    }
                    default:
                        throw new Exception("Unhandled action in Process");
                }
            }
        }

        public static string GetTargetID(string modEntryPath)
        {
            var merge = ModTek.ParseGameJSONFile(modEntryPath);
            return merge[nameof(AdvancedJSONMerge.TargetID)].ToString();
        }

        private static bool IsAdvancedJSONMerge(JObject merge)
        {
            return merge[nameof(AdvancedJSONMerge.TargetID)] != null;
        }

        private static void DoAdvancedMerge(JObject target, JObject merge)
        {
            var instructions = merge[nameof(AdvancedJSONMerge.Instructions)].ToObject<List<AdvancedMergeInstruction>>();
            foreach (var instruction in instructions)
                instruction.Process(target);
        }

        public static void MergeIntoTarget(JObject target, JObject merge)
        {
            if (IsAdvancedJSONMerge(merge))
                DoAdvancedMerge(target, merge);
            else
                target.Merge(merge, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
        }

        // unused, this level is parsed manually
#pragma warning disable CS0649
        private class AdvancedJSONMerge
        {
            [JsonProperty(Required = Required.Always)]
            public string TargetID;

            [JsonProperty(Required = Required.Always)]
            public List<AdvancedMergeInstruction> Instructions;
        }
#pragma warning restore
    }
}
