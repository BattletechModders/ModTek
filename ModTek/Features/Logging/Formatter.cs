using System;
using System.Globalization;
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
        // only improving TRACE level already brings all the benefits
        // but we would have 2 different formats in the same log file
        //!LogLevelExtension.IsLogLevelGreaterThan(LogLevel.Log, messageDto.LogLevel);
        var fastLogging = _settings.FastLoggingEnabled;

        var sb = new StringBuilder();

        if (fastLogging)
        {
            if (_settings.AbsoluteTimeEnabled || _settings.StartupTimeEnabled)
            {
                var ts = messageDto.StartupTime();
                var secondsWithFraction = ts.Ticks * 1E-07m;
                sb.Append(secondsWithFraction);
                sb.Append(" ");
            }
        }
        else
        {
            if (_settings.AbsoluteTimeEnabled)
            {
                var dt = messageDto.GetDateTime();
                var dts = _settings.AbsoluteTimeUseUtc ? dt : dt.ToLocalTime();
                sb.Append(dts.ToString(_settings.AbsoluteFormat, CultureInfo.InvariantCulture));
                sb.Append(" ");
            }

            if (_settings.StartupTimeEnabled)
            {
                var ts = messageDto.StartupTime();
                sb.Append(ts.ToString(_settings.StartupTimeFormat));
                sb.Append(" ");
            }
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
            if (!fastLogging && _sanitizerRegex != null)
            {
                sb.Append(_sanitizerRegex.Replace(messageDto.Message, string.Empty));
            }
            else
            {
                sb.Append(messageDto.Message);
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

        if (!fastLogging)
        {
            if (_settings.NormalizeNewLines)
            {
                sb.Replace("\r", "");
                sb.Replace("\n", Environment.NewLine);
            }

            if (_settings.IndentNewLines)
            {
                sb.Replace("\n", "\n\t");
            }
        }

        sb.Append(Environment.NewLine);

        return sb.ToString();
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