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

        if (_settings.AbsoluteTimeEnabled)
        {
            var dtoUtc = messageDto.GetDateTimeOffsetUtc();
            var dto = _settings.AbsoluteTimeUseUtc ? dtoUtc : dtoUtc.ToLocalTime();
            sb.Append(dto.ToString(_settings.AbsoluteFormat, CultureInfo.InvariantCulture));
            sb.Append(" ");
        }

        if (_settings.StartupTimeEnabled)
        {
            var ts = messageDto.StartupTime();
            sb.Append(ts.ToString(_settings.StartupTimeFormat));
            sb.Append(" ");
        }

        if (messageDto.thread != null)
        {
            sb.Append("[ThreadId=");
            sb.Append(messageDto.thread.ManagedThreadId);
            sb.Append("] ");
        }

        sb.Append(messageDto.loggerName);
        sb.Append(" ");

        sb.Append("[");
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
            try
            {
                sb.Append(messageDto.location.ToString());
            }
            catch (Exception e)
            {
                sb.Append(e);
            }
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
}