﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace ModTek.Features.Logging;

internal class LoggingSettings
{
    [JsonProperty]
    internal const string DebugLogDumpServerListen_Description = "HTTP server to force logs dump";
    [JsonProperty]
    internal string DebugLogDumpServerListen = string.Empty;

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
    internal const string LogSqlQueryExecutionsFromDapper_Description = "Logs out any sql queries being executed via Dapper, logs actual parameters.";
    [JsonProperty]
    internal bool LogSqlQueryExecutionsFromDapper;

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
    internal const string DebugLogLevelSetters_Description = "Log who changed a log level changed.";
    [JsonProperty]
    internal bool DebugLogLevelSetters;

    [JsonProperty]
    internal const string UnityConsoleAppenderEnabled_Description = "Append HBS log statements to the unity console. Disabled by default as it reduces performance.";
    [JsonProperty]
    internal bool UnityConsoleAppenderEnabled;

    [JsonProperty]
    internal const string ModLogAppendersEnabled_Description = "Allows mods to configure log appenders, set to false to improve performance.";
    [JsonProperty]
    internal bool ModLogAppendersEnabled = true;

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
    internal const string LogFlushToDisk_Description = "Makes ILog.Flush() flush logs to disk. Blocks until completed.";
    [JsonProperty]
    internal bool LogFlushToDisk = true;

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
                Include = ["ModTek"]
            }
        }
    };
}