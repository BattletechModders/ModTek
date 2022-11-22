using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ModTek.Features.Logging;

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
        var sb = new StringBuilder();

        sb.Append(GetFormattedTime(messageDto));

        if (messageDto.thread != null)
        {
            sb.Append(" [ThreadId=");
            sb.Append(messageDto.thread.ManagedThreadId);
            sb.Append("]");
        }

        sb.Append(" ");
        sb.Append(messageDto.loggerName);

        sb.Append(" [");
        sb.Append(LogLevelExtension.LogToString(messageDto.logLevel));
        sb.Append("]");

        var prefix = " ";
        if (!string.IsNullOrEmpty(messageDto.message))
        {
            sb.Append(prefix);
            if (_sanitizerRegex == null)
            {
                sb.Append(messageDto.message);
            }
            else
            {
                sb.Append(_sanitizerRegex.Replace(messageDto.message, string.Empty));
            }
            prefix = Environment.NewLine;
        }

        if (messageDto.exception != null)
        {
            sb.Append(prefix);
            sb.Append(messageDto.exception);
            prefix = Environment.NewLine;
        }

        if (messageDto.location != null)
        {
            sb.Append(prefix);
            sb.Append("Location Trace");
            sb.Append(Environment.NewLine);
            sb.Append(messageDto.location.ToString());
        }

        var line = sb.ToString();

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
            return dto.ToString(_settings.FormatTimeAbsolute, CultureInfo.InvariantCulture);
        }

        var ts = messageDto.StartupTime();
        return ts.ToString(_settings.FormatTimeStartup);
    }
}