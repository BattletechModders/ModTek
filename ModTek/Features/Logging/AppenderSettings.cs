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
    internal const string IndentNewLines_Description = "If a log message with newlines inside is being logged, prefix every line with a tab character.";
    [JsonProperty]
    internal bool IndentNewLines = true;

    [JsonProperty]
    internal const string NormalizeNewLines_Description = "Makes sure all newline characters ([\\r\\n]) are converted to the OS preferred newline style.";
    [JsonProperty]
    internal bool NormalizeNewLines = true;

    [JsonProperty]
    internal readonly string MessageSanitizerRegex_Description = "The characters to remove from the log message before writing to the disk.";
    [JsonProperty]
    internal string MessageSanitizerRegex = @"[\p{C}-[\r\n\t]]+";

    [JsonProperty]
    internal const string AbsoluteTimeEnabled_Description = "Adds the clock time.";
    [JsonProperty]
    internal bool AbsoluteTimeEnabled = true;

    [JsonProperty]
    internal const string AbsoluteTimeUseUtc_Description = "Use UTC instead of local time.";
    [JsonProperty]
    internal bool AbsoluteTimeUseUtc = true;

    [JsonProperty]
    internal const string AbsoluteFormat_Description = "Runs through `DateTimeOffset.ToString`.";
    [JsonProperty]
    internal string AbsoluteFormat = "HH:mm:ss.fffffff";

    [JsonProperty]
    internal const string StartupTimeEnabled_Description = "Adds the time since startup.";
    [JsonProperty]
    internal bool StartupTimeEnabled;

    [JsonProperty]
    internal const string StartupTimeFormat_Description = "Runs through `TimeSpan.ToString`.";
    [JsonProperty]
    internal string StartupTimeFormat = "hh':'mm':'ss'.'fffffff";

    [JsonProperty]
    internal const string FastLoggingEnabled_Description = "If enabled, hardcodes formats and disables sanitation to achieve high performance.";
    [JsonProperty]
    internal bool FastLoggingEnabled = true;
}