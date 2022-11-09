using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging
{
    internal class Formatter
    {
        private static readonly DateTimeOffset StartupTime;
        static Formatter()
        {
            StartupTime = DateTimeOffset.UtcNow.AddSeconds(-Time.realtimeSinceStartup);
        }

        private readonly FormatterSettings settings;
        public Formatter(FormatterSettings settings)
        {
            this.settings = settings;
        }

        internal LogLine GetFormattedLogLine(MTLoggerMessageDto messageDto)
        {
            var formattedTime = GetFormattedTime(messageDto.time);
            var formattedThread = GetFormattedThread(messageDto.thread);
            var messageString = messageDto.message == null
                ? string.Empty
                : MessageSanitizer.Replace(messageDto.message, string.Empty);
            var line = string.Format(settings.FormatLine,
                messageDto.loggerName,
                LogToString(messageDto.logLevel),
                messageString,
                messageDto.exception == null ?  null : GetFormattedException(messageDto.exception)
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

            return new LogLine(formattedTime, formattedThread, line);
        }

        // used to remove logging unfriendly characters
        private static readonly Regex MessageSanitizer = new Regex(@"[\p{C}-[\r\n\t]]+", RegexOptions.Compiled);

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

        private string GetFormattedTime(DateTimeOffset time)
        {
            return settings.UseAbsoluteTime ? GetFormattedAbsoluteTime(time) : GetFormattedStartupTime(time);
        }

        private string GetFormattedStartupTime(DateTimeOffset time)
        {
            var value = time - StartupTime;
            var formatted = string.Format(
                settings.FormatStartupTime,
                value.Hours,
                value.Minutes,
                value.Seconds,
                value.Milliseconds);
            return formatted;
        }

        private string GetFormattedAbsoluteTime(DateTimeOffset time)
        {
            var dto = settings.FormatAbsoluteTimeUseUtc ? time.ToUniversalTime() : time.ToLocalTime();
            return dto.ToString(settings.FormatAbsoluteTime, CultureInfo.InvariantCulture);
        }

        private string GetFormattedThread(Thread thread)
        {
            if (thread == null)
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
    }
}