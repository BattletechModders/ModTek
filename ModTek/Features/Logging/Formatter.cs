using System;
using System.Diagnostics;
using System.Globalization;
using Harmony;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging
{
    internal class Formatter
    {
        private readonly FormatterSettings settings;
        public Formatter(FormatterSettings settings)
        {
            this.settings = settings;
        }

        internal LogLine GetFormattedLogLine(string name, LogLevel logLevel, object message, Exception exception, IStackTrace location)
        {
            var time = GetFormattedTime();
            var line = string.Format(settings.FormatLine,
                name,
                LogToString(logLevel),
                message,
                exception == null ?  GetFormattedLocation(location) : GetFormattedException(exception)
            );

            if (settings.NormalizeNewLines)
            {
                line = line.Replace("\r", "");
                line = line.Replace("\n", Environment.NewLine);
            }

            if (settings.IndentNewLines)
            {
                line = line.Replace("\n", "\n\t");
            }

            var timeWithLine = string.Format(settings.FormatTimeAndLine, time, line);

            return new LogLine(timeWithLine, line);
        }

        internal class LogLine
        {
            public LogLine(string line, string lineWithoutTime)
            {
                Line = line;
                LineWithoutTime = lineWithoutTime;
            }

            internal string Line { get; }
            internal string LineWithoutTime { get; }
        }

        private string GetFormattedTime()
        {
            return settings.UseAbsoluteTime ? GetFormattedAbsoluteTime() : GetFormattedStartupTime();
        }

        private string GetFormattedStartupTime()
        {
            var value = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
            var formatted = string.Format(
                settings.FormatStartupTime,
                value.Hours,
                value.Minutes,
                value.Seconds,
                value.Milliseconds);
            return formatted;
        }

        private string GetFormattedAbsoluteTime()
        {
            return DateTime.UtcNow.ToString(settings.FormatAbsoluteTime, CultureInfo.InvariantCulture);
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
                    return "WARNING";
                case LogLevel.Error:
                    return "ERROR";
                default:
                    return "?????";
            }
        }

        private string GetFormattedException(Exception exception)
        {
            return string.Format(settings.FormatException, exception);
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
            var formatted = string.Format(settings.FormatLocation, method.DeclaringType, method.Name);
            return formatted;
        }
    }
}