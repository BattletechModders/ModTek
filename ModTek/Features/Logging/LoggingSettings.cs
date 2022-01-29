using Newtonsoft.Json;

namespace ModTek.Features.Logging
{
    internal class LoggingSettings
    {
        [JsonProperty]
        internal string[] PrefixesToIgnore = {};
        [JsonProperty]
        internal const string PrefixesToIgnore_Description = "Ignore any lines starting with any of the listed prefixes.";

        [JsonProperty]
        internal bool PreserveFullLog;
        [JsonProperty]
        internal const string PreserveFullLog_Description = "Preserve a complete log where prefixes are not ignored.";

        [JsonProperty]
        internal bool LogUncaughtExceptions = true;
        [JsonProperty]
        internal const string LogUncaughtExceptions_Description = "Logs uncaught exceptions via AppDomain handler.";

        [JsonProperty]
        internal bool IgnoreLoggerLogLevel;
        [JsonProperty]
        internal const string IgnoreLoggerLogLevel_Description = "Each logger has a log level, and when logging below that level it won't be logged. That behavior can be ignored to a certain extend. Set to true for FYLS behavior, not recommended though.";

        [JsonProperty]
        internal bool DebugLogLevelSetters;
        [JsonProperty]
        internal const string DebugLogLevelSetters_Description = "Log if a loggers log level changed and by whom.";

        [JsonProperty]
        internal bool SkipOriginalLoggers;
        [JsonProperty]
        internal const string SkipOriginalLoggers_Description = "If true, the original (HBS based) loggers and therefore their appenders and log files will be skipped.";

        [JsonProperty]
        internal string[] IgnoreSkipForLoggers = {"ModTek"};
        [JsonProperty]
        internal const string IgnoreSkipForLoggers_Description = "Loggers defined here will never be skipped, meaning their log files will still be separately available.";

        [JsonProperty]
        internal FormatterSettings ModTekLogFormatting = new FormatterSettings
        {
            FormatLine = "[{1}] {2}{3}" // log level is always LOG and we know its ModTek
        };

        [JsonProperty]
        internal FormatterSettings BattleTechLogFormatting = new FormatterSettings();
    }
}
