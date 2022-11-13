using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace ModTek.Features.Logging
{
    internal class Formatter
    {
        private readonly AppenderSettings _settings;
        private readonly Regex _sanitizerRegex;

        internal Formatter(AppenderSettings settings)
        {
            _settings = settings;
            _sanitizerRegex = new Regex(settings.MessageSanitizerRegex, RegexOptions.Compiled);
        }

        internal string GetFormattedLogLine(MTLoggerMessageDto messageDto)
        {
            var formattedTime = GetFormattedTime(messageDto);
            var formattedThread = GetFormattedThread(messageDto.thread);
            var formattedLogLevel = LogLevelExtension.LogToString(messageDto.logLevel);

            string messageString;
            if (string.IsNullOrEmpty(messageDto.message))
            {
                messageString = string.Empty;
            }
            else if (_sanitizerRegex != null)
            {
                messageString = _sanitizerRegex.Replace(messageDto.message, string.Empty);
            }
            else
            {
                messageString = messageDto.message;
            }

            var exceptionAddition = messageDto.exception == null ? null : $": {messageDto.exception}";
            var threadAddition = formattedThread == null ? null : " " + formattedThread;
            var line = $"{formattedTime}{threadAddition} {messageDto.loggerName} [{formattedLogLevel}] {messageString}{exceptionAddition}";

            if (_settings.NormalizeNewLines)
            {
                line = line.Replace("\r", "");
                line = line.Replace("\n", Environment.NewLine);
            }

            if (_settings.IndentNewLines)
            {
                line = line.Replace("\n", "\n\t");
            }

            return line;
        }

        private string GetFormattedTime(MTLoggerMessageDto messageDto)
        {
            if (_settings.UseAbsoluteTime)
            {
                var dtoUtc = messageDto.GetDateTimeOffsetUtc();
                var dto = _settings.AbsoluteTimeUseUtc ? dtoUtc : dtoUtc.ToLocalTime();
                return dto.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
            }

            var ts = messageDto.StartupTime();
            return ts.ToString("hh':'mm':'ss'.'fffffff");
        }

        private string GetFormattedThread(Thread thread)
        {
            if (thread == null)
            {
                return null;
            }
            return "[ThreadId=" + thread.ManagedThreadId + "]";
        }
    }
}