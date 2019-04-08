using System;
using System.Diagnostics;
using Harmony;
using HBS.Logging;
using UnityEngine;
using Logger = HBS.Logging.Logger;
using Object = UnityEngine.Object;

namespace ModTek.Logging
{
    public class BetterLogFormatter
    {
        public string LogLineFormat { get; set; } = "{0} [{1}] {2} {3}{4}";

        internal string GetFormattedLogLine(string name, LogLevel logLevel, object message, Object context, Exception exception, IStackTrace location)
        {
            var line = string.Format(LogLineFormat,
                GetFormattedStartupTime(),
                GetFormattedLogLevel(logLevel),
                GetFormattedName(name),
                GetFormattedMessage(message),
                exception == null ?  GetFormattedLocation(location) : GetFormattedException(exception)
            );

            return line;
        }

        public string StartupTimeFormat { get; set; } = "{1:D2}:{2:D2}.{3:D3}";

        private string GetFormattedStartupTime()
        {
            var value = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
            var formatted = string.Format(
                StartupTimeFormat,
                value.Hours,
                value.Minutes,
                value.Seconds,
                value.Milliseconds);
            return formatted;
        }

        public string LogLevelFormat { get; set; } = "{0,-5}";

        private string GetFormattedLogLevel(LogLevel logLevel)
        {
            var text = LogToString(logLevel);
            var formatted = string.Format(LogLevelFormat, text);
            return formatted;
        }

        private string LogToString(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    return "DEBUG";
                case LogLevel.Log:
                    return "LOG";
                case LogLevel.Warning:
                    return "WARN";
                case LogLevel.Error:
                    return "ERROR";
                default:
                    return "?????";
            }
        }

        public string NameFormat { get; set; } = "{0,5}";

        private string GetFormattedName(string name)
        {
            var formatted = string.Format(NameFormat, name);
            return formatted;
        }

        public string MessageFormat { get; set; } = "{0}";

        private string GetFormattedMessage(object message)
        {
            var formatted = string.Format(MessageFormat, message);
            return formatted;
        }

        public string ExceptionFormat { get; set; } = " Exception: {0}";

        private string GetFormattedException(Exception exception)
        {
            var formatted = string.Format(ExceptionFormat, exception);
            return formatted;
        }

        public string LocationFormat { get; set; } = " [{0}.{1}]";

        private string GetFormattedLocation(IStackTrace location)
        {
            if (!Logger.IsStackTraceEnabled || location == null || location.FrameCount < 1)
            {
                return null;
            }

            var stackTrace = Traverse.Create((DiagnosticsStackTrace)location)?.Field<StackTrace>("stackTrace")?.Value;
            if (stackTrace == null || stackTrace.FrameCount < 1)
            {
                return null;
            }
            var method = stackTrace.GetFrame(0).GetMethod();
            var formatted = string.Format(LocationFormat, method.DeclaringType, method.Name);
            return formatted;
        }
    }
}