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
        private static Regex IgnorePrefixesMatcher;
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
                    var ignoredPrefixesPattern = $"^(?:{string.Join("|", prefixes.OrderBy(m => m).Select(Regex.Escape))})";
                    IgnorePrefixesMatcher = new Regex(ignoredPrefixesPattern);
                }
                else
                {
                    IgnorePrefixesMatcher = null;
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

        // private static readonly TickCounter overheadPrefixMatcher = new TickCounter();
        // used for intercepting all logging attempts and to log centrally
        internal static void LogAtLevel(string loggerName, LogLevel logLevel, object message, Exception exception, IStackTrace location)
        {
            var logLine = btLogFormatter.GetFormattedLogLine(
                loggerName,
                logLevel,
                message,
                exception,
                location
            );

            btFullAppender?.WriteLine(logLine.Line);

            if (IgnorePrefixesMatcher != null)
            {
                // var tracker = new TickTracker();
                // tracker.Begin();
                var ignore = IgnorePrefixesMatcher.IsMatch(logLine.LineWithoutTime);
                // tracker.End();
                // overheadPrefixMatcher.IncrementBy(tracker);
                if (ignore)
                {
                    return;
                }
            }
            btCleanAppender.WriteLine(logLine.Line);
            // btCleanAppender.WriteLine("overhead prefix matcher " + overheadPrefixMatcher);
        }
    }
}
