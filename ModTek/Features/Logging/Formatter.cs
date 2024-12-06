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

    private readonly StringBuilder _sb = new(128 * 1024);
    internal string GetFormattedLogLine(MTLoggerMessageDto messageDto)
    {
        var fastLogging = _settings.FastLoggingEnabled;

        _sb.Clear();

        if (fastLogging)
        {
            if (_settings.AbsoluteTimeEnabled)
            {
                var dt = messageDto.GetDateTime();
                void AppendTwoDigits(int value)
                {
                    _sb.Append((char)(value / 10 + '0'));
                    _sb.Append((char)(value % 10 + '0'));
                }
                AppendTwoDigits(dt.Hour);
                _sb.Append(":");
                AppendTwoDigits(dt.Minute);
                _sb.Append(":");
                AppendTwoDigits(dt.Second);
                _sb.Append(".");
                var ticks = dt.Ticks;
                _sb.Append((char)(((ticks / 1_000_000) % 10) + '0'));
                _sb.Append((char)(((ticks / 100_000) % 10) + '0'));
                _sb.Append((char)(((ticks / 10_000) % 10) + '0'));
                _sb.Append((char)(((ticks / 1_000) % 10) + '0'));
                _sb.Append((char)(((ticks / 100) % 10) + '0'));
                _sb.Append((char)(((ticks / 10) % 10) + '0'));
                _sb.Append((char)(((ticks / 1) % 10) + '0'));
                _sb.Append(" ");
            }

            if (_settings.StartupTimeEnabled)
            {
                var ts = messageDto.StartupTime();
                var secondsWithFraction = ts.Ticks * 1E-07m;
                _sb.Append(secondsWithFraction);
                _sb.Append(" ");
            }
        }
        else
        {
            if (_settings.AbsoluteTimeEnabled)
            {
                var dt = messageDto.GetDateTime();
                var dts = _settings.AbsoluteTimeUseUtc ? dt : dt.ToLocalTime();
                _sb.Append(dts.ToString(_settings.AbsoluteFormat, CultureInfo.InvariantCulture));
                _sb.Append(" ");
            }

            if (_settings.StartupTimeEnabled)
            {
                var ts = messageDto.StartupTime();
                _sb.Append(ts.ToString(_settings.StartupTimeFormat));
                _sb.Append(" ");
            }
        }

        if (messageDto.ThreadId != s_unityMainThreadId)
        {
            _sb.Append("[ThreadId=");
            _sb.Append(messageDto.ThreadId);
            _sb.Append("] ");
        }

        _sb.Append(messageDto.LoggerName);
        _sb.Append(" ");

        _sb.Append("[");
        _sb.Append(LogLevelExtension.LogToString(messageDto.LogLevel));
        _sb.Append("]");

        var prefix = " ";
        if (!string.IsNullOrEmpty(messageDto.Message))
        {
            _sb.Append(prefix);
            if (!fastLogging && _sanitizerRegex != null)
            {
                _sb.Append(_sanitizerRegex.Replace(messageDto.Message, string.Empty));
            }
            else
            {
                _sb.Append(messageDto.Message);
            }
            prefix = Environment.NewLine;
        }

        if (messageDto.Exception != null)
        {
            _sb.Append(prefix);
            _sb.Append(messageDto.Exception);
            prefix = Environment.NewLine;
        }

        if (messageDto.Location != null)
        {
            _sb.Append(prefix);
            _sb.Append("Location Trace");
            _sb.Append(Environment.NewLine);
            _sb.Append(GetLocationString(messageDto.Location));
        }

        if (!fastLogging)
        {
            if (_settings.NormalizeNewLines)
            {
                _sb.Replace("\r", "");
                _sb.Replace("\n", Environment.NewLine);
            }

            if (_settings.IndentNewLines)
            {
                _sb.Replace("\n", "\n\t");
            }
        }

        _sb.Append(Environment.NewLine);

        return _sb.ToString();
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