using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace ModTek.AdvMerge
{
    internal class Instruction
    {
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        internal MergeAction Action;

        [JsonProperty(Required = Required.Always)]
        public string JSONPath;

        public JToken Value;

        public bool Process(JObject root)
        {
            var jTokens = root.SelectTokens(JSONPath).ToList();

            if (jTokens.Count == 0)
            {
                return false;
            }

            foreach (var jToken in jTokens)
            {
                switch (Action)
                {
                    case MergeAction.Remove:
                    {
                        if (jToken.Parent is JProperty)
                        {
                            jToken.Parent.Remove();
                        }
                        else
                        {
                            jToken.Remove();
                        }

                        break;
                    }
                    case MergeAction.Replace:
                    {
                        jToken.Replace(Value);
                        break;
                    }
                    case MergeAction.ArrayAdd:
                    {
                        if (!(jToken is JArray jArray))
                        {
                            throw new Exception("JSONPath needs to point an array");
                        }

                        jArray.Add(Value);
                        break;
                    }
                    case MergeAction.ArrayAddAfter:
                    {
                        jToken.AddAfterSelf(Value);
                        break;
                    }
                    case MergeAction.ArrayAddBefore:
                    {
                        jToken.AddBeforeSelf(Value);
                        break;
                    }
                    case MergeAction.ObjectMerge:
                    {
                        if (!(jToken is JObject jObject1) || !(Value is JObject jObject2))
                        {
                            throw new Exception("JSONPath has to point to an object and Value has to be an object");
                        }

                        // same behavior as partial json merging
                        jObject1.Merge(jObject2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                        break;
                    }
                    case MergeAction.ArrayConcat:
                    {
                        if (!(jToken is JArray jArray1) || !(Value is JArray jArray2))
                        {
                            throw new Exception("JSONPath has to point to an array and Value has to be an array");
                        }

                        jArray1.Merge(jArray2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                        break;
                    }
                    default:
                    {
                        throw new Exception("Unhandled action in Process");
                    }
                }
            }

            return true;
        }
    }
}
