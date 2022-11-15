using System.Collections.Generic;
using HBS.Logging;
using Newtonsoft.Json;

namespace ModTek.Features.Logging;

internal class FilterSettings
{
    [JsonProperty]
    internal const string _Description = "The filter matches if all subfiles accept the message.";

    [JsonProperty]
    internal const string LogLevels_Description = "If set, this subfilter rejects the message if no value matches.";
    [JsonProperty]
    internal LogLevel[] LogLevels;

    [JsonProperty]
    internal const string LoggerNames_Description = "If set, this subfilter rejects the message if no value matches.";
    [JsonProperty]
    internal string[] LoggerNames;

    [JsonProperty]
    internal const string MessagePrefixes_Description = "If set, this subfilter rejects the message if no value matches.";
    [JsonProperty]
    internal string[] MessagePrefixes;

    public override string ToString()
    {
        var ret = new List<string>();
        if (LoggerNames != null)
        {
            ret.Add("LoggerNames[" + string.Join(",", LoggerNames) + "]");
        }
        if (LogLevels != null)
        {
            ret.Add("LogLevels[" + string.Join(",", LogLevels) + "]");
        }
        if (MessagePrefixes != null)
        {
            ret.Add("MessagePrefixes[" + string.Join(",", MessagePrefixes) + "]");
        }
        return "FilterSettings[" + string.Join(",", ret) + "]";
    }
}