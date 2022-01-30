using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Harmony;
using HBS.Logging;
using ModTek.Util;
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

        internal LogLine GetFormattedLogLine(string name, LogLevel logLevel, object message, Exception exception, IStackTrace location, Thread thread)
        {
            var time = GetFormattedTime();
            var formattedThread = GetFormattedThread(thread);
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

            return new LogLine(time, formattedThread, line);
        }

        internal class LogLine
        {
            public LogLine(string time, string thread, string line)
            {
                if (thread == null)
                {
                    FullLine = time + " " + line;
                }
                else
                {
                    FullLine = time + " " + thread + " " + line;
                }
                PrefixLine = line;
            }

            internal string FullLine { get; }
            internal string PrefixLine { get; }
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

        private string GetFormattedThread(Thread thread)
        {
            if (MTUnityUtils.MainManagedThreadId == thread.ManagedThreadId)
            {
                return null;
            }
            return "[ThreadId=" + thread.ManagedThreadId + "]";
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