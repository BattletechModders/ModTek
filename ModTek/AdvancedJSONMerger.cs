
using System;
using System.Collections.Generic;
using System.Linq;
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
            ArrayAdd, // adds a given value to the end of the target array
            ArrayAddAfter, // adds a given value after the target element in the array
            ArrayAddBefore, // adds a given value before the target element in the array
            ArrayConcat, // adds a given array to the end of the target array
            ObjectMerge, // merges a given object with the target objects
            Remove, // removes the target element(s)
            Replace, // replaces the target with a given value
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
                var tokens = root.SelectTokens(JSONPath).ToList();
                if (!tokens.Any())
                {
                    throw new Exception("JSONPath does not point to anything");
                }

                if (ProcessTokens(tokens))
                {
                    return;
                }

                if (tokens.Count > 1)
                {
                    throw new Exception("JSONPath can't point to more than one token outside of the Remove action");
                }

                if (ProcessToken(tokens[0]))
                {
                    return;
                }

                throw new Exception("Action is unknown");
            }

            private bool ProcessTokens(List<JToken> tokens)
            {
                if (Action == Action.Remove)
                {
                    foreach (var token in tokens)
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

                    return true;
                }

                return false;
            }

            private bool ProcessToken(JToken token)
            {
                if (Action == Action.Replace)
                {
                    token.Replace(Value);
                    return true;
                }

                if (Action == Action.ArrayAdd)
                {
                    if (!(token is JArray a))
                    {
                        throw new Exception("JSONPath needs to point an array");
                    }

                    a.Add(Value);
                    return true;
                }

                if (Action == Action.ArrayAddAfter)
                {
                    token.AddAfterSelf(Value);
                    return true;
                }

                if (Action == Action.ArrayAddBefore)
                {
                    token.AddBeforeSelf(Value);
                    return true;
                }

                if (Action == Action.ObjectMerge)
                {
                    if (!(token is JObject o1) || !(Value is JObject o2))
                    {
                        throw new Exception("JSONPath has to point to an object and Value has to be an object");
                    }

                    // same behavior as partial json merging
                    o1.Merge(o2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                    return true;
                }

                if (Action == Action.ArrayConcat)
                {
                    if (!(token is JArray a1) || !(Value is JArray a2))
                    {
                        throw new Exception("JSONPath has to point to an array and Value has to be an array");
                    }

                    a1.Merge(a2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                    return true;
                }

                return false;
            }
        }
    }
}
