using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using fastJSON;
using HBS.Collections;
using HBS.Logging;
using HBS.Util;
using ModTek.Features.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Utilities;

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
        return JsonConvert.SerializeObject(target, s_jsonSerializerSettings);
    }

    // does not work 100%, issues e.g. with MechDef and ChassisRefresh
    // idea is to be able to replace JSONSerializationUtility_FromJSON
    // not because its faster, its slower (1-2s -> 4-6s), but because we can then start using other frameworks as well
    // one of the others might be faster, does not need to be json
    internal static void PopulateObject(object target, string json)
    {
        var commentsStripped = JSONSerializationUtility.StripHBSCommentsFromJSON(json);
        var commasAdded = FixHBSJsonCommas(commentsStripped);
        JsonConvert.PopulateObject(commasAdded, target, s_jsonSerializerSettings);
    }

    private static readonly JsonSerializerSettings s_jsonSerializerSettings = new()
    {
        ContractResolver = new FastJsonContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        Converters = [
            new TagSetConverter(),
            new FastJsonStringEnumConverter(),
            new FastJsonDictionaryConverter(),
            new DecimalJsonConverter(),
        ],
        // TraceWriter = new JsonTraceWriter(),
        // SerializationBinder = new SerializationBinderAdapter(new DefaultSerializationBinder())
    };

    private class JsonTraceWriter : ITraceWriter
    {
        public void Trace(TraceLevel level, string message, Exception ex)
        {
            if (level == TraceLevel.Off)
            {
                return;
            }

            Log.Main.Log.LogAtLevel(ConvertLevel(level), message, ex);
        }

        private static LogLevel ConvertLevel(TraceLevel level)
        {
            return level switch
            {
                TraceLevel.Error => LogLevel.Error,
                TraceLevel.Warning => LogLevel.Warning,
                TraceLevel.Info => LogLevel.Log,
                TraceLevel.Verbose => LogLevel.Debug,
                _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
            };
        }

        public TraceLevel LevelFilter => LogLevelExtension.GetLevelFilter(Log.Main.Log);
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
                objectType.GetProperties( BindingFlags.Instance | BindingFlags.Public)
                    .Where(member => member.CanWrite && member.CanRead)
                    .Where(member => !member.IsDefined(typeof(JsonIgnore), false))
            );
            members.AddRange(
                objectType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(member => member.IsDefined(typeof(JsonSerialized), false))
            );

            members.AddRange(
                objectType.GetFields( BindingFlags.Instance | BindingFlags.Public)
                    .Where(member => !member.IsDefined(typeof(JsonIgnore), false))
            );
            members.AddRange(
                objectType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(member => !member.IsInitOnly)
                    .Where(member => member.IsDefined(typeof(JsonSerialized), false))
            );

            // Order of properties & fields processing is different between fastJson+HBS compared to Newtonsoft.Json
            // -> some setters are called in different order
            // -> sometimes different things are null or not at a different time
            // solutions:
            // - emulate fastJson+HBS ordering? -> uses some kind of 1. properties 2. fields but not always
            // - fix vanilla code to avoid needing setters at all? -> can break existing code
            // - introduce explicit converters? -> similar to CustomPrewam; require mod support APIs to let mods fix themselves
            Log.Main.Debug?.Log($"Found {members.Count} members for type {objectType.FullName}: {string.Join(",", members.Select(m => m.Name))}");

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

    private class FastJsonStringEnumConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value.ToString());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var isNullable = ReflectionUtils.IsNullableType(objectType);
            if (reader.TokenType == JsonToken.Null)
            {
                if (!isNullable)
                    throw JsonSerializationException.Create(reader, "Cannot convert null value to {0}.".FormatWith(CultureInfo.InvariantCulture, objectType));
                return null;
            }

            var enumType = isNullable ? Nullable.GetUnderlyingType(objectType) : objectType;
            if (enumType == null)
            {
                throw new InvalidCastException();
            }

            if (reader.TokenType == JsonToken.String)
            {
                try
                {
                    var value = reader.Value.ToString();
                    if (value.Length == 0 && isNullable)
                    {
                        return null;
                    }
                    return Enum.Parse(enumType, value, true);
                }
                catch (Exception ex)
                {
                    throw JsonSerializationException.Create(reader, "Error converting value {0} to type '{1}'.".FormatWith(CultureInfo.InvariantCulture, MiscellaneousUtils.FormatValueForPrint(reader.Value), objectType), ex);
                }
            }
            if (reader.TokenType == JsonToken.Integer)
            {
                var numericValue = (int)reader.Value;
                return Enum.ToObject(enumType, numericValue);
            }

            throw JsonSerializationException.Create(reader, "Unexpected token {0} when parsing enum.".FormatWith(CultureInfo.InvariantCulture, reader.TokenType));
        }

        public override bool CanConvert(Type objectType)
        {
            // copied from StringEnumConverter.CanConvert
            return (ReflectionUtils.IsNullableType(objectType) ? Nullable.GetUnderlyingType(objectType) : objectType).IsEnum();
        }
    }

    /*
     * { "items": [ I ], "tagSetSourceFile": ... }
     * to
     * [ I ]
     */
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

    /*
     * [ { "k" : "A" , "v" : B } , { "k" : "C" , "v": D } ]
     * to
     * { "A" : B , "C" : D }
     */
    private class FastJsonDictionaryConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var valueType = objectType.GetGenericArguments()[1];
            var intermediateDictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
            var intermediateDictionary = (IDictionary)Activator.CreateInstance(intermediateDictionaryType);

            if (reader.TokenType == JsonToken.StartArray)
            {
                var intermediateListItemType = typeof(FastJsonDictionaryArrayItem<>).MakeGenericType(valueType);
                var intermediateListType = typeof(List<>).MakeGenericType(intermediateListItemType);
                var intermediateList = (IList)Activator.CreateInstance(intermediateListType);
                serializer.Populate(reader, intermediateList);
                foreach (var item in intermediateList)
                {
                    var traverse = Traverse.Create(item);
                    var key = traverse.Field("k").GetValue();
                    var value = traverse.Field("v").GetValue();
                    intermediateDictionary.Add(key, value);
                }
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                serializer.Populate(reader, intermediateDictionary);
            }

            var keyType = objectType.GetGenericArguments()[0];
            if (keyType == typeof(string))
            {
                return intermediateDictionary;
            }

            var finalDictionary = (IDictionary)Activator.CreateInstance(objectType);
            if (keyType.IsEnum)
            {
                foreach (DictionaryEntry pair in intermediateDictionary)
                {
                    var key = Enum.Parse(keyType, (string)pair.Key, true);
                    finalDictionary.Add(key, pair.Value);
                }
            }
            else
            {
                throw JsonSerializationException.Create(reader, $"Dictionary key type {keyType} is not supported.");

            }
            return finalDictionary;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary).IsAssignableFrom(objectType);
        }
    }
    private class FastJsonDictionaryArrayItem<T>
    {
        public string k;
        public T v;
    }

    // don't serialize after decimal points if those are 0
    private class DecimalJsonConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal) || objectType == typeof(float) || objectType == typeof(double);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(Format(value));
        }

        private static string Format(object value)
        {
            // BT was first written for 32bit, therefore more floats exist
            if (value is float floatValue)
            {
                return floatValue.ToString("G9", CultureInfo.InvariantCulture);
            }
            if (value is double doubleValue)
            {
                return doubleValue.ToString("G17", CultureInfo.InvariantCulture);
            }
            var formattableValue = value as IFormattable;
            return formattableValue!.ToString("R", CultureInfo.InvariantCulture);
        }
    }
}