using System.Collections.Generic;
using HBS.Logging;
using Newtonsoft.Json;

namespace ModTek.Features.Logging;

internal class FilterSettings
{
    [JsonProperty]
    internal const string _Description = "The filter matches if all subfiles accept the message.";

    [JsonProperty]
    internal const string LoggerName_Description = "If set, this subfilter rejects the message if the value does not match.";
    [JsonProperty]
    internal string LoggerName;

    [JsonProperty]
    internal const string LogLevel_Description = "If set, this subfilter rejects the message if the value does not match.";
    [JsonProperty]
    internal LogLevel? LogLevel;

    [JsonProperty]
    internal const string MessagePrefix_Description = "If set, this subfilter rejects the message if the value does not match.";
    [JsonProperty]
    internal string MessagePrefix;

    public override string ToString()
    {
        var ret = new List<string>();
        if (LoggerName != null)
        {
            ret.Add($"LoggerName[{LoggerName}]");
        }
        if (LogLevel != null)
        {
            ret.Add($"LogLevel[{LogLevel}]");
        }
        if (MessagePrefix != null)
        {
            ret.Add($"MessagePrefix[{MessagePrefix}]");
        }
        return "FilterSettings[" + string.Join(",", ret) + "]";
    }
}