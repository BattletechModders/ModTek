using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Harmony;
using HBS.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModTek.Logging
{
    public class BetterLogFormatter
    {
        private readonly BetterLogFormatterSettings settings;
        public BetterLogFormatter(BetterLogFormatterSettings settings)
        {
            this.settings = settings;
        }

        internal string GetFormattedLogLine(string name, LogLevel logLevel, object message, Object context, Exception exception, IStackTrace location)
        {
            var line = string.Format(settings.LogLineFormat,
                GetFormattedTime(),
                GetFormattedLogLevel(logLevel),
                GetFormattedName(name),
                GetFormattedMessage(message),
                exception == null ?  GetFormattedLocation(location) : GetFormattedException(exception)
            );

            if (settings.NormalizeNewLines)
            {
                line = NEWLINE_REGEX.Replace(line, Environment.NewLine);
            }

            if (settings.IndentNewLines)
            {
                line = NEWLINE_REGEX.Replace(line, Environment.NewLine + "\t");
            }

            return line;
        }
        
        private static readonly Regex NEWLINE_REGEX = new Regex(@"\r\n|\n\r|\n|\r", RegexOptions.Compiled);

        private string GetFormattedTime()
        {
            return settings.UseAbsoluteTime ? GetFormattedAbsoluteTime() : GetFormattedStartupTime();
        }

        private string GetFormattedStartupTime()
        {
            var value = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
            var formatted = string.Format(
                settings.StartupTimeFormat,
                value.Hours,
                value.Minutes,
                value.Seconds,
                value.Milliseconds);
            return formatted;
        }

        internal string GetFormattedAbsoluteTime()
        {
            return DateTime.UtcNow.ToString(settings.AbsoluteTimeFormat, System.Globalization.CultureInfo.InvariantCulture);
        }

        private string GetFormattedLogLevel(LogLevel logLevel)
        {
            var text = LogToString(logLevel);
            var formatted = string.Format(settings.LogLevelFormat, text);
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

        private string GetFormattedName(string name)
        {
            var formatted = string.Format(settings.NameFormat, name);
            return formatted;
        }

        private string GetFormattedMessage(object message)
        {
            var formatted = string.Format(settings.MessageFormat, message);
            return formatted;
        }

        private string GetFormattedException(Exception exception)
        {
            var formatted = string.Format(settings.ExceptionFormat, exception);
            return formatted;
        }

        private string GetFormattedLocation(IStackTrace location)
        {
            if (!HBS.Logging.Logger.IsStackTraceEnabled || location == null || location.FrameCount < 1)
            {
                return null;
            }

            var stackTrace = Traverse.Create((DiagnosticsStackTrace)location)?.Field<StackTrace>("stackTrace")?.Value;
            if (stackTrace == null || stackTrace.FrameCount < 1)
            {
                return null;
            }
            var method = stackTrace.GetFrame(0).GetMethod();
            var formatted = string.Format(settings.LocationFormat, method.DeclaringType, method.Name);
            return formatted;
        }
    }
}