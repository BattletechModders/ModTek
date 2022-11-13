using System;
using System.Collections.Generic;
using System.IO;
using HBS.Logging;
using ModTek.Misc;

namespace ModTek.Features.Logging
{
    internal static class LoggingFeature
    {
        private static readonly ILog ModTekLogger = Logger.GetLogger("ModTek", LogLevel.Debug);

        private static LoggingSettings Settings => ModTek.Config.Logging;

        private static LogAppender _mainLog;
        private static readonly List<LogAppender> _logsAppenders = new List<LogAppender>();

        private static MTLoggerAsyncQueue _queue;

        internal static void Init()
        {
            Directory.CreateDirectory(FilePaths.TempModTekDirectory);

            _mainLog = new LogAppender(Settings.MainLogFilePath, Settings.MainLog);
            foreach (var kv in Settings.Logs)
            {
                _logsAppenders.Add(new LogAppender(kv.Key, kv.Value));
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

            _queue = new MTLoggerAsyncQueue(ProcessLoggerMessage);
        }

        // used for direct logging from ModTek code
        internal static void Log(LogLevel logLevel, string message, Exception e)
        {
            ModTekLogger.LogAtLevel(logLevel, message, e);
        }

        // used for intercepting all logging attempts and to log centrally
        internal static void LogAtLevel(string loggerName, LogLevel logLevel, object message, Exception exception, IStackTrace location)
        {
            var messageDto = new MTLoggerMessageDto(loggerName, logLevel, message, exception);
            if (!_queue.Add(messageDto))
            {
                ProcessLoggerMessage(messageDto);
            }
        }

        // note this can be called sync or async
        private static void ProcessLoggerMessage(MTLoggerMessageDto messageDto)
        {
            _mainLog.Append(messageDto);
            foreach (var logAppender in _logsAppenders)
            {
                logAppender.Append(messageDto);
            }
        }

        internal static void WriteExceptionToFatalLog(Exception exception)
        {
            File.WriteAllText(Path.Combine("Mods", ".modtek", "ModTekFatalError.log"), exception.ToString());
        }
    }
}
