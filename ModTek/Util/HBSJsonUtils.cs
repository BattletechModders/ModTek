using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using fastJSON;
using HBS.Collections;
using HBS.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ModTek.Util;

internal static class HBSJsonUtils
{
    internal static JObject ParseGameJSONFile(string path, bool log = false)
    {
        var content = File.ReadAllText(path);
        return ParseGameJSON(content, log);
    }

    internal static JObject ParseGameJSON(string content, bool log = false)
    {
        Log.Main.Info?.LogIf(log,"content: " + content);

        try
        {
            return JObject.Parse(content);
        }
        catch (Exception)
        {
            // ignored
        }

        var commentsStripped = JSONSerializationUtility.StripHBSCommentsFromJSON(content);
        Log.Main.Info?.LogIf(log, "commentsStripped: " + commentsStripped);

        var commasAdded = FixHBSJsonCommas(commentsStripped);
        Log.Main.Info?.LogIf(log,"commasAdded: " + commasAdded);

        return JObject.Parse(commasAdded);
    }

    private static readonly Regex s_fixMissingCommasInJson = new(
        """(\]|\}|"|[A-Za-z0-9])\s*\n\s*(\[|\{|")""",
        RegexOptions.Singleline|RegexOptions.Compiled
    );
    private static string FixHBSJsonCommas(string json)
    {
        // add missing commas, this only fixes if there is a newline
        return s_fixMissingCommasInJson.Replace(json, "$1,\n$2");
    }

    // might work, only slightly tested
    internal static string SerializeObject(object target)
    {
        return JsonConvert.SerializeObject(
            target,
            new JsonSerializerSettings
            {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                ContractResolver = new FastJsonContractResolver()
            }
        );
    }

    // does not work 100%, issues e.g. with MechDef and ChassisRefresh
    // idea is to be able to replace JSONSerializationUtility_FromJSON
    // not because its faster, its slower (1-2s -> 4-6s), but because we can then start using other frameworks as well
    // one of the others might be faster, does not need to be json
    internal static void PopulateObject(object target, string json)
    {
        var commentsStripped = JSONSerializationUtility.StripHBSCommentsFromJSON(json);
        var commasAdded = FixHBSJsonCommas(commentsStripped);
        var dictionariesFixed = FixStructures(commasAdded);
        JsonConvert.PopulateObject(dictionariesFixed, target, new JsonSerializerSettings
        {
            ContractResolver = new FastJsonContractResolver(),
            Converters = [new TagSetConverter()]
        });
    }

    private static string FixStructures(string json)
    {
        var newton = JObject.Parse(json);
        FindAndFixStructures(newton);
        return newton.ToString();
    }

    private static void FindAndFixStructures(JToken newton)
    {
        switch (newton)
        {
            case JArray a:
                var dict = ConvertHbsDictArrayToDictionary(a);
                if (dict != null)
                {
                    foreach (var v in dict.Values())
                    {
                        FindAndFixStructures(v);
                    }
                }

                return;
            case JObject o:
                if (ConvertHbsTagsToArray(o))
                {
                    return;
                }
                foreach (var v in o.Values())
                {
                    FindAndFixStructures(v);
                }
                break;
        }
    }

    /*
     * [ { "k" : "A" , "v" : B } , { "k" : "C" , "v": D } ]
     * { "A" : B , "C" : D }
     */
    // similar to TagSetConverter
    private static JObject ConvertHbsDictArrayToDictionary(JArray a)
    {
        var newDict = new JObject();
        if (a.Count <= 1)
        {
            return null;
        }

        foreach (var i in a)
        {
            if (i is not JObject o)
            {
                return null;
            }

            if (!ExtractKeyValue(o, out var key, out var value))
            {
                return null;
            }

            if (newDict[key] != null)
            {
                return null;
            }

            newDict.Add(key, value);
        }

        a.Replace(newDict);

        return newDict;
    }

    /*
     * { "items": [ I ], "tagSetSourceFile": ... }
     * to
     * [ I ]
     */
    private static bool ConvertHbsTagsToArray(JObject o)
    {
        if (o.Count is >= 1 and <= 2)
        {
            if (o.Count == 2)
            {
                if (!o.TryGetValue("tagSetSourceFile", out _))
                {
                    return false;
                }
            }
            if (o.TryGetValue("items", out var items) && items.Type == JTokenType.Array)
            {
                o.Replace(items);
            }
        }
        return false;
    }

    private static bool ExtractKeyValue(
        JObject o,
        [NotNullWhen(true)] out string keyAsString,
        [NotNullWhen(true)] out JToken valueAsToken
    ) {
        if (o.Count == 2)
        {
            if (o.TryGetValue("k", out var keyAsToken) && keyAsToken.Type == JTokenType.String)
            {
                if (keyAsToken is JValue keyAsValue)
                {
                    keyAsString = keyAsValue._value as string;
                    if (o.TryGetValue("v", out valueAsToken))
                    {
                        return true;
                    }
                }
            }
        }
        keyAsString = null;
        valueAsToken = null;
        return false;
    }


    private class FastJsonContractResolver : DefaultContractResolver
    {
        private readonly Dictionary<Type, List<MemberInfo>> _serializableMembersCache = new();
        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            var members = new List<MemberInfo>();
            lock (_serializableMembersCache)
            {
                if (_serializableMembersCache.TryGetValue(objectType, out var serializableMembers))
                {
                    return serializableMembers;
                }
            }

            members.AddRange(
                objectType.GetFields(Reflection.defaultFlags & ~BindingFlags.Static)
                    .Where(member => !member.IsDefined(typeof(JsonIgnore), false))
            );
            members.AddRange(
                objectType.GetFields(Reflection.privateFlags)
                    .Where(member => !member.IsInitOnly)
                    .Where(member => member.IsDefined(typeof(JsonSerialized), false))
            );

            members.AddRange(
                objectType.GetProperties(Reflection.defaultFlags & ~BindingFlags.Static)
                    .Where(member => member.CanWrite && member.CanRead)
                    .Where(member => !member.IsDefined(typeof(JsonIgnore), false))
            );
            members.AddRange(
                objectType.GetProperties(Reflection.privateFlags)
                    .Where(member => member.IsDefined(typeof(JsonSerialized), false))
            );

            // if (objectType == typeof(MechDef))
            // {
            //     Log.Main.Debug?.Log($"Found {members.Count} members for type {objectType.FullName}: {string.Join(",", members.Select(m => m.Name))}");
            // }

            lock (_serializableMembersCache)
            {
                _serializableMembersCache[objectType] = members;
            }

            return members;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            // if (member.DeclaringType != null && member.DeclaringType.IsAssignableFrom(typeof(MechDef)))
            // {
            //     Log.Main.Debug?.Log($"Found member {member} on MechDef");
            // }

            var property = base.CreateProperty(member, memberSerialization);
            property.Readable = true;
            property.Writable = true;
            property.Ignored = false;
            property.ShouldSerialize = _ => true;
            property.ShouldDeserialize = _ => true;
            return property;
        }
    }

    // similar to ConvertHbsDictArrayToDictionary
    private class TagSetConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is TagSet tagSet)
            {
                writer.WriteStartArray();
                foreach (var item in tagSet.items)
                {
                    writer.WriteValue(item);
                }
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JArray array;
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.StartObject:
                    var jObject = JObject.Load(reader);
                    if (!jObject.TryGetValue("items", out var token) || token is not JArray castArray)
                    {
                        return null;
                    }
                    array = castArray;
                    break;
                case JsonToken.StartArray:
                    array = JArray.Load(reader);
                    break;
                default:
                    return null;
            }
            List<string> items = new();
            foreach (var item in array)
            {
                if (item.Type != JTokenType.String)
                {
                    return null;
                }
                items.Add(item.ToString());
            }
            return new TagSet(items);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TagSet);
        }
    }
}