using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using HBS.Logging;
using ModTek.Misc;

namespace ModTek.Features.Logging
{
    internal static class LoggingFeature
    {
        private static readonly ILog MainLogger = Logger.GetLogger("ModTek");

        private static LoggingSettings Settings => ModTek.Config.Logging;
        private static Regex IgnorePrefixesMatcher;

        private static string BTCleanLogFilePath => Path.Combine(FilePaths.TempModTekDirectory, "battletech_log.txt");
        private static string BTFullLogFilePath => Path.Combine(FilePaths.TempModTekDirectory, "battletech_log_original.txt");
        private static string MTLogFilePath => FilePaths.LogPath;

        private static Formatter btLogFormatter;
        private static Appender btCleanAppender;
        private static Appender btFullAppender;

        private static Formatter modTekFormatter;
        private static Appender modTekAppender;

        private static MTLoggerAsyncQueue queue;

        internal static void Init()
        {
            Directory.CreateDirectory(FilePaths.TempModTekDirectory);

            btLogFormatter = new Formatter(Settings.BattleTechLogFormatting);
            btCleanAppender = new Appender(BTCleanLogFilePath);
            btFullAppender = Settings.PreserveFullLog ? new Appender(BTFullLogFilePath) : null;

            modTekFormatter = new Formatter(Settings.ModTekLogFormatting);
            modTekAppender = new Appender(MTLogFilePath);
            Logger.SetLoggerLevel(MainLogger.Name, LogLevel.Debug);

            {
                var prefixes = Settings.PrefixesToIgnore;
                if (prefixes.Any())
                {
                    try
                    {
                        var trie = Trie.Create(prefixes);
                        IgnorePrefixesMatcher = trie.CompileRegex();
                    }
                    catch (Exception e)
                    {
                        btCleanAppender.WriteLine("Issue processing logging ignore prefixes" + e);
                    }
                }
                else
                {
                    IgnorePrefixesMatcher = null;
                }
            }

            if (Settings.LogUncaughtExceptions)
            {
                AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                {
                    var message = "UnhandledException";
                    if (!(e.ExceptionObject is Exception ex))
                    {
                        ex = null;
                        message += " " + e.ExceptionObject;
                    }

                    LogAtLevel(
                        "AppDomain",
                        LogLevel.Debug,
                        message,
                        ex,
                        null
                    );
                };
            }

            queue = new MTLoggerAsyncQueue(ProcessLoggerMessage);
        }

        // used for direct logging from ModTek code
        internal static void Log(LogLevel logLevel, string message, Exception e)
        {
            MainLogger.LogAtLevel(logLevel, message, e);
        }

        // used for intercepting all logging attempts and to log centrally
        internal static void LogAtLevel(string loggerName, LogLevel logLevel, object message, Exception exception, IStackTrace location)
        {
            var messageDto = new MTLoggerMessageDto(loggerName, logLevel, message, exception);
            if (!queue.Add(messageDto))
            {
                ProcessLoggerMessage(messageDto);
            }
        }

        // note this can be called sync or async
        private static void ProcessLoggerMessage(MTLoggerMessageDto messageDto)
        {
            {
                var logLine = btLogFormatter.GetFormattedLogLine(messageDto);

                btFullAppender?.WriteLine(logLine.FullLine);

                // TODO use message dto fields directly
                // TODO avoids the need to assemble a prefix line
                // TODO meaning formatting could be skipped (~50% of logging time is spent during formatting)
                if (IgnorePrefixesMatcher == null || !IgnorePrefixesMatcher.IsMatch(logLine.PrefixLine))
                {
                    btCleanAppender.WriteLine(logLine.FullLine);
                }
            }

            // TODO allow to add other loggers and log file paths
            // TODO multiple logger names incl wildcard + formatter settings => filename to put into .modtek/logs
            if (messageDto.loggerName == "ModTek")
            {
                var logLine = modTekFormatter.GetFormattedLogLine(messageDto);
                modTekAppender.WriteLine(logLine.FullLine);
            }
        }
    }
}
