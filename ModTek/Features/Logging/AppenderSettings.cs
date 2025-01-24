using Newtonsoft.Json;

namespace ModTek.Features.Logging;

internal class AppenderSettings
{
    [JsonProperty]
    internal const string LogRotationCount_Description = "How many log file to backups between application starts.";
    [JsonProperty]
    internal int LogRotationCount = 1;

    [JsonProperty]
    internal const string Include_Description = "If set, matching log statements by prefix are written to the log. Exclusion has precedence over inclusion.";
    [JsonProperty]
    internal string[] Include;

    [JsonProperty]
    internal const string Exclude_Description = "If set, matching log statements by prefix are ignored. Exclusion has precedence over inclusion.";
    [JsonProperty]
    internal string[] Exclude;

    [JsonProperty]
    internal const string AbsoluteTimeEnabled_Description = "Adds the clock time.";
    [JsonProperty]
    internal bool AbsoluteTimeEnabled = true;

    [JsonProperty]
    internal const string AbsoluteTimeUseUtc_Description = "Use UTC instead of local time.";
    [JsonProperty]
    internal bool AbsoluteTimeUseUtc = true;

    [JsonProperty]
    internal const string StartupTimeEnabled_Description = "Adds the time since startup.";
    [JsonProperty]
    internal bool StartupTimeEnabled;
}