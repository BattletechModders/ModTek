using System.Collections.Generic;
using HBS.Logging;
using Newtonsoft.Json;

namespace ModTek.Features.Logging
{
    internal class LoggingSettings
    {
        [JsonProperty]
        internal const string LogUncaughtExceptions_Description = "Logs uncaught exceptions via AppDomain handler.";
        [JsonProperty]
        internal bool LogUncaughtExceptions = true;

        [JsonProperty]
        internal const string LogExceptionInitializations_Description = "Logs out any exceptions being initialized, even before they are thrown or ignored.";
        [JsonProperty]
        internal bool LogExceptionInitializations;

        [JsonProperty]
        internal const string LogSqlQueryInitializations_Description = "Logs out any sql queries being initialized, even before they are executed or ignored.";
        [JsonProperty]
        internal bool LogSqlQueryInitializations;

        [JsonProperty]
        internal bool LogThreadStarts = true;
        [JsonProperty]
        internal const string LogThreadStarts_Description = "Logs who starts threads.";

        [JsonProperty]
        internal const string IgnoreLoggerLogLevel_Description = "Each logger has a log level, and when logging below that level it won't be logged. That behavior can be ignored to a certain extend. Set to true for FYLS behavior, not recommended though.";
        [JsonProperty]
        internal bool IgnoreLoggerLogLevel;

        [JsonProperty]
        internal const string DebugLogLevelSetters_Description = "Log if a loggers log level changed and by whom.";
        [JsonProperty]
        internal bool DebugLogLevelSetters;

        [JsonProperty]
        internal const string SkipOriginalLoggers_Description = "If true, the original (HBS based) loggers and therefore their appenders and log files will be skipped.";
        [JsonProperty]
        internal bool SkipOriginalLoggers = true;

        [JsonProperty]
        internal const string IgnoreSkipForLoggers_Description = "Loggers defined here will never be skipped, meaning their log files will still be separately available.";
        [JsonProperty(Required = Required.DisallowNull)]
        internal string[] IgnoreSkipForLoggers = {};

        [JsonProperty]
        internal const string MainLog_Description = "The main log.";
        [JsonProperty(Required = Required.Always)]
        internal AppenderSettings MainLog = new AppenderSettings();
        [JsonProperty(Required = Required.Always)]
        internal string MainLogFilePath = "battletech_log.txt";

        [JsonProperty]
        internal const string Logs_Description = "Allows to define logs, the `key` specifies the log file path relative to `.modtek`.";
        [JsonProperty(Required = Required.Always)]
        internal Dictionary<string, AppenderSettings> Logs = new Dictionary<string, AppenderSettings>
        {
            {
                "ModTek.log",
                new AppenderSettings
                {
                    Includes = new[]
                    {
                        new FilterSettings
                        {
                            LoggerNames = new[] { "ModTek" }
                        }
                    }
                }
            }
        };
    }
}
