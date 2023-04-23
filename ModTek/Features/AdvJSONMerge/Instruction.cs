using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Linq.JsonPath;

namespace ModTek.Features.AdvJSONMerge;

internal class Instruction
{
    [JsonProperty(Required = Required.Always)]
    [JsonConverter(typeof(StringEnumConverter))]
    internal MergeAction Action;

    [JsonProperty(Required = Required.Always)]
    public string JSONPath;

    [JsonProperty]
    public JToken Value;

    [JsonProperty]
    public bool AutoCreateProperty;

    public void Process(JObject root)
    {
        var jTokens = GetOrCreateJTokens(root);
        if (jTokens.Count == 0)
        {
            throw new Exception("Did not find anything");
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
                    if (jToken is not JArray jArray)
                    {
                        throw new Exception("JSONPath needs to point to an array");
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
                    if (jToken is not JObject jObject1 || Value is not JObject jObject2)
                    {
                        throw new Exception("JSONPath has to point to an object and Value has to be an object");
                    }

                    // same behavior as partial json merging
                    jObject1.Merge(jObject2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
                    break;
                }
                case MergeAction.ArrayConcat:
                {
                    if (jToken is not JArray jArray1 || Value is not JArray jArray2)
                    {
                        throw new Exception("JSONPath has to point to an array and Value has to be an array");
                    }

                    jArray1.Merge(jArray2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Concat });
                    break;
                }
                default:
                {
                    throw new Exception("Unhandled action");
                }
            }
        }
    }

    private List<JToken> GetOrCreateJTokens(JToken root)
    {
        var jPath = new JPath(JSONPath);

        if (!AutoCreateProperty)
        {
            return JPath.Evaluate(jPath.Filters, root, root, false).ToList();
        }

        var filterCount = jPath.Filters.Count;
        if (filterCount < 1)
        {
            throw new Exception($"{nameof(AutoCreateProperty)}: JSONPath does not contain a field property expression");
        }

        var lastIndex = filterCount - 1;
        FieldFilter fieldFilter;
        {
            fieldFilter = jPath.Filters[lastIndex] as FieldFilter;
            if (fieldFilter?.Name == null)
            {
                throw new Exception($"{nameof(AutoCreateProperty)}: JSONPath does not contain a field property expression at the end");
            }
        }

        IEnumerable<JToken> tmpTokens = new[]
        {
            root
        };
        for (var index = 0; index < lastIndex; index++)
        {
            var filter = jPath.Filters[index];
            tmpTokens = filter.ExecuteFilter(root, tmpTokens, false);
        }

        var defaultValue = GetDefaultValueForAction();

        var tokens = new List<JToken>();
        foreach (var parentToken in tmpTokens)
        {
            var found = fieldFilter.ExecuteFilter(root, new[] { parentToken }, false).SingleOrDefault();
            if (found == null)
            {
                if (parentToken is not JObject parentObject)
                {
                    throw new Exception($"{nameof(AutoCreateProperty)}: The container is not an object and does not accept properties");
                }
                var property = new JProperty(fieldFilter.Name)
                {
                    Value = defaultValue
                };
                parentObject.Add(property);
                tokens.Add(property.Value);
            }
            else
            {
                tokens.Add(found);
            }
        }

        return tokens;
    }

    private JToken GetDefaultValueForAction()
    {
        switch (Action)
        {
            case MergeAction.ArrayAdd:
            case MergeAction.ArrayConcat:
                return new JArray();
            case MergeAction.Replace: // doesn't matter, will be replaced anyway
            case MergeAction.ObjectMerge:
                return new JObject();
            default:
                throw new Exception($"{nameof(AutoCreateProperty)}: The merge action is not supported");
        }
    }
}