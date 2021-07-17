using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HBS.Logging;
using ModTek.Misc;

namespace ModTek.Features.Logging
{
    internal static class LoggingFeature
    {
        private static readonly ILog MainLogger = Logger.GetLogger("ModTek");

        private static LoggingSettings Settings => ModTek.Config.Logging;
        private static Regex LogPrefixesMatcher;
        private static Formatter btLogFormatter;

        private static string BTCleanLogFilePath => Path.Combine(FilePaths.TempModTekDirectory, "battletech_log.txt");
        private static string BTFullLogFilePath => Path.Combine(FilePaths.TempModTekDirectory, "battletech_log_original.txt");
        private static string MTLogFilePath => FilePaths.LogPath;

        private static Appender btCleanAppender;
        private static Appender btFullAppender;

        internal static void Init()
        {
            btLogFormatter = new Formatter(Settings.BattleTechLogFormatting);

            Directory.CreateDirectory(FilePaths.TempModTekDirectory);
            var appender = new Appender(MTLogFilePath, Settings.ModTekLogFormatting);
            Logger.AddAppender(MainLogger.Name, appender);
            Logger.SetLoggerLevel(MainLogger.Name, LogLevel.Log);

            {
                var prefixes = Settings.PrefixesToIgnore;
                if (prefixes.Any())
                {
                    var ignoredPrefixesPattern = $"^(?:{string.Join("|", prefixes.Select(Regex.Escape))})";
                    LogPrefixesMatcher = new Regex(ignoredPrefixesPattern);
                }
                else
                {
                    LogPrefixesMatcher = new Regex("^$");
                }
            }

            btCleanAppender = new Appender(BTCleanLogFilePath);
            btFullAppender = Settings.PreserveFullLog ? new Appender(BTFullLogFilePath) : null;
        }

        // used for direct logging from ModTek code
        internal static void Log(string message, Exception e)
        {
            MainLogger.Log(message, e);
        }

        // used for intercepting all logging attempts and to log centrally
        internal static void LogAtLevel(string loggerName, LogLevel logLevel, object message, Exception exception, IStackTrace location, out bool skipOriginal)
        {
            skipOriginal = Settings.SkipOriginalLoggers && !Settings.IgnoreSkipForLoggers.Contains(loggerName);

            if (!Settings.IgnoreLoggerLogLevel && !HasLoggerLogLevelEnabled(loggerName, logLevel))
            {
                return;
            }

            var logLine = btLogFormatter.GetFormattedLogLine(
                loggerName,
                logLevel,
                message,
                exception,
                location
            );

            btFullAppender?.WriteLine(logLine.Line);

            if (!LogPrefixesMatcher.IsMatch(logLine.LineWithoutTime))
            {
                btCleanAppender.WriteLine(logLine.Line);
            }
        }

        private static bool HasLoggerLogLevelEnabled(string name, LogLevel level)
        {
            var log = Logger.GetLogger(name);
            switch (level)
            {
                case LogLevel.Debug when log.IsDebugEnabled:
                case LogLevel.Log when log.IsLogEnabled:
                case LogLevel.Warning when log.IsWarningEnabled:
                case LogLevel.Error when log.IsErrorEnabled:
                    return true;
                default:
                    return false;
            }
        }
    }
}
