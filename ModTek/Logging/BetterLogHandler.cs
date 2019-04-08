using System;
using System.IO;
using HBS.Logging;
using Logger = HBS.Logging.Logger;
using Object = UnityEngine.Object;

namespace ModTek.Logging
{
    internal class BetterLogHandler
    {
        internal static BetterLogHandler Shared = new BetterLogHandler();

        private ILogAppender cleanAppender;
        private LogLevel logLevel;
        //private bool ignoreLoggerLogLevel = true; // logger log level is hidden

        internal void SetupCleanedLog(string path, BetterLogSettings settings, bool enableStackTraceLogging)
        {
            cleanAppender = new BetterLogFileAppender(path, settings.Formatter, settings.PatternsToIgnore);
            logLevel = settings.LogLevel;
            Logger.IsStackTraceEnabled = enableStackTraceLogging;
        }

        internal void SetupModLog(ModDef modDef)
        {
            var settings = modDef.LogSettings;
            var loggerName = modDef.Name;
            var logPath = Path.Combine(modDef.Directory, "log.txt");
                    
            if (modDef.LogSettings.LogFileEnabled)
            {
                var appender = new BetterLogFileAppender(logPath, settings.Formatter, settings.PatternsToIgnore);
                Logger.AddAppender(loggerName, appender);
            }
            Logger.SetLoggerLevel(loggerName, settings.LogLevel);
        }

        internal void OnLogMessage(string name, LogLevel level, object message, Object context, Exception exception, IStackTrace location)
        {
            if (cleanAppender == null || !Logger.IsLogging || level < logLevel)
            {
                return;
            }

            cleanAppender.OnLogMessage(name, level, message, context, exception, location);
        }
    }
}