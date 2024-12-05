using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HBS.Logging;

namespace ModTek.Features.Logging;

internal class Formatter
{
    // we assume the formatter is first accessed when we are on the unity main thread
    private static readonly int s_unityMainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
    
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

        if (messageDto.ThreadId != s_unityMainThreadId)
        {
            sb.Append("[ThreadId=");
            sb.Append(messageDto.ThreadId);
            sb.Append("] ");
        }

        sb.Append(messageDto.LoggerName);
        sb.Append(" ");

        sb.Append("[");
        sb.Append(LogLevelExtension.LogToString(messageDto.LogLevel));
        sb.Append("]");

        var prefix = " ";
        if (!string.IsNullOrEmpty(messageDto.Message))
        {
            sb.Append(prefix);
            if (_sanitizerRegex == null)
            {
                sb.Append(messageDto.Message);
            }
            else
            {
                sb.Append(_sanitizerRegex.Replace(messageDto.Message, string.Empty));
            }
            prefix = Environment.NewLine;
        }

        if (messageDto.Exception != null)
        {
            sb.Append(prefix);
            sb.Append(messageDto.Exception);
            prefix = Environment.NewLine;
        }

        if (messageDto.Location != null)
        {
            sb.Append(prefix);
            sb.Append("Location Trace");
            sb.Append(Environment.NewLine);
            sb.Append(GetLocationString(messageDto.Location));
        }

        sb.Append(Environment.NewLine);

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

    private static string GetLocationString(IStackTrace st)
    {
        try
        {
            return st switch
            {
                UnityStackTrace ust => ust.stackTrace,
                _ => st.ToString()
            };
        }
        catch (Exception e)
        {
            return e.ToString();
        }
    }
}