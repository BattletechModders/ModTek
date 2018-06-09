
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace ModTek
{
    public static class AdvancedJSONMerger
    {
        public static string GetTargetFile(string modEntryPath)
        {
            var merge = ModTek.ParseGameJSONFile(modEntryPath);
            return merge["TargetFile"].ToString();
        }

        public static bool IsAdvancedJSONMerge(JObject merge)
        {
            return merge["TargetFile"] != null;
        }

        public static void ProcessInstructionsJObject(JObject target, JObject merge)
        {
            // TODO add nice error handling (on malformed JSONPath)
            var instructions = merge["Instructions"].ToObject<List<Instruction>>();
            foreach (var instruction in instructions)
            {
                // TODO add nice error handling (which JSONPath failed)
                instruction.Process(target);
            }
        }

        // unused, this level is parsed manually
        private class MergeFile
        {
            [JsonProperty(Required = Required.Always)]
            public string TargetFile;

            [JsonProperty(Required = Required.Always)]
            public List<Instruction> Instructions;
        }

        public enum Action
        {
            Add, // is equivalent to AddAfter of last array element
            AddAfter, // mainly arrays, usable but useless for objects
            AddBefore, // mainly arrays, usable but useless for objects
            Concat, // only arrays
            Merge, // only objects
            Remove, // remove tokens from objects or arrays
            Replace, // replace tokens in objects or arrays
        }

        public class Instruction
        {
            [JsonProperty(Required = Required.Always)]
            public string JSONPath;

            [JsonProperty(Required = Required.Always)]
            [JsonConverter(typeof(StringEnumConverter))]
            public Action Action;

            // TODO add external JSON support
            public JToken Value;

            public void Process(JObject root)
            {
                var token = root.SelectToken(JSONPath);
                if (token == null)
                {
                    throw new Exception("JSONPath does not point to anything");
                }

                if (Action == Action.Replace)
                {
                    token.Replace(Value);
                }
                else if (Action == Action.Remove)
                {
                    if (token.Parent is JProperty)
                    {
                        token.Parent.Remove();
                    }
                    else
                    {
                        token.Remove();
                    }
                }
                else if (Action == Action.Add)
                {
                    if (token is JArray a)
                    {
                        a.Add(Value);
                    }
                    else
                    {
                        throw new Exception("JSONPath needs to point an array");
                    }
                }
                else if (Action == Action.AddAfter)
                {
                    token.AddAfterSelf(Value);
                }
                else if (Action == Action.AddBefore)
                {
                    token.AddBeforeSelf(Value);
                }
                else if (Action == Action.Merge)
                {
                    if (token is JObject o1 && Value is JObject o2)
                    {
                        // same behavior as partial json merging
                        o1.Merge(o2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                    }
                    else
                    {
                        throw new Exception("JSONPath has to point to an object and Value has to be an object");
                    }
                }
                else if (Action == Action.Concat)
                {
                    if (token is JArray a1 && Value is JArray a2)
                    {
                        a1.Merge(a2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                    }
                    else
                    {
                        throw new Exception("JSONPath has to point to an array and Value has to be an array");
                    }
                }
                else
                {
                    throw new Exception("Action is unknown");
                }
            }
        }

    }
}
