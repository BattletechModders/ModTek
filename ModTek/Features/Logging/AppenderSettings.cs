using Newtonsoft.Json;

namespace ModTek.Features.Logging;

internal class AppenderSettings
{
    [JsonProperty]
    internal const string LogRotationCount_Description = "How many log file to backups between application starts.";
    [JsonProperty]
    internal int LogRotationCount = 1;

    [JsonProperty]
    internal const string Includes_Description = "If set, matching log statements matching are written to the log. Exclusion has precedence over inclusion.";
    [JsonProperty]
    internal FilterSettings[] Includes;

    [JsonProperty]
    internal const string Excludes_Description = "If set, matching log statements are ignored. Exclusion has precedence over inclusion.";
    [JsonProperty]
    internal FilterSettings[] Excludes;

    [JsonProperty]
    internal readonly string PrefixesToIgnore_Description = $"Ignore any lines starting with any of the listed prefixes, internally will be converted to {nameof(Excludes)}.";
    [JsonProperty]
    internal string[] PrefixesToIgnore;

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