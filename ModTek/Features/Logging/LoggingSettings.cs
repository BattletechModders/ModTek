using System.Collections.Generic;
using HBS.Logging;
using Newtonsoft.Json;
using NullableLogging;

namespace ModTek.Features.Logging;

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
    internal const string LogThreadStarts_Description = "Logs who starts threads.";
    [JsonProperty]
    internal bool LogThreadStarts = true;

    [JsonProperty]
    internal const string LogStackTraces_Description = "Logs environment stack traces for every log statement, produces large amount of text.";
    [JsonProperty]
    internal bool LogStackTraces;

    [JsonProperty]
    internal const string LogStackTracesOnExceptions_Description = "Logs environment stack traces in addition to exception stack traces.";
    [JsonProperty]
    internal bool LogStackTracesOnExceptions = true;

    [JsonProperty]
    internal const string OverrideLoggerLevels_Description = "Overrides the log levels for the given loggers.";
    [JsonProperty]
    internal Dictionary<string, LogLevel> OverrideLoggerLevels = new()
    {
        { nameof(AppenderUnityConsole.UnityLoggerName), LogLevel.Debug },
        { nameof(Log.Debugger), LogLevel.Debug },
        { nameof(Log.AppDomain), LogLevel.Debug },
        { nameof(ModTek), NullableLogger.TraceLogLevel }
    };

    [JsonProperty]
    internal const string IgnoreLoggerLogLevel_Description = "Each logger has a log level, and when logging below that level it won't be logged. That behavior can be ignored to a certain extend. Set to true for FYLS behavior, not recommended though.";
    [JsonProperty]
    internal bool IgnoreLoggerLogLevel;

    [JsonProperty]
    internal const string DebugLogLevelSetters_Description = "Log who changed a log level changed.";
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
    internal const string UnityConsoleAppenderEnabled_Description = "Append HBS log statements to the unity console, slows down logging and spams the console.";
    [JsonProperty]
    internal bool UnityConsoleAppenderEnabled;

    [JsonProperty]
    internal const string UnityConsoleAppender_Description = "Settings for the unity console appender.";
    [JsonProperty]
    internal AppenderSettings UnityConsoleAppender = new()
    {
        AbsoluteTimeEnabled = false,
        StartupTimeEnabled = false,
    };

    [JsonProperty]
    internal const string AsynchronousLoggingEnabled_Description = "Uses another thread to format and log messages off the main thread.";
    [JsonProperty]
    internal bool AsynchronousLoggingEnabled = true;

    [JsonProperty]
    internal const string MainLog_Description = "The main log.";
    [JsonProperty(Required = Required.Always)]
    internal AppenderSettings MainLog = new();
    [JsonProperty(Required = Required.Always)]
    internal string MainLogFilePath = "battletech_log.txt";

    [JsonProperty]
    internal const string Logs_Description = "Allows to define logs, the `key` specifies the log file path relative to `.modtek`.";
    [JsonProperty(Required = Required.Always)]
    internal Dictionary<string, AppenderSettings> Logs = new()
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