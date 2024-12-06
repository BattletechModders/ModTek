using System;
using System.Globalization;
using HBS.Logging;

namespace ModTek.Features.Logging;

internal class Formatter
{
    private static readonly int s_unityMainThreadId = UnityEngine.Object.CurrentThreadIsMainThread()
        ? System.Threading.Thread.CurrentThread.ManagedThreadId
        : 1;

    private readonly bool _absoluteTimeEnabled;
    private readonly bool _absoluteTimeUseUtc;
    private readonly bool _startupTimeEnabled;

    internal Formatter(AppenderSettings settings)
    {
        _absoluteTimeEnabled = settings.AbsoluteTimeEnabled;
        _absoluteTimeUseUtc = settings.AbsoluteTimeUseUtc;
        _startupTimeEnabled = settings.StartupTimeEnabled;
    }

    [ThreadStatic]
    private static FastBuffer s_buffer;

    internal int GetFormattedLogLine(ref MTLoggerMessageDto messageDto, out byte[] bytes)
    {
        s_buffer ??= new FastBuffer();
        s_buffer.Clear();

        if (_absoluteTimeEnabled)
        {
            var dt = messageDto.GetDateTime();
            if (!_absoluteTimeUseUtc)
            {
                dt = dt.ToLocalTime();
            }
            s_buffer.AppendLast2Digits(dt.Hour);
            s_buffer.Append(':');
            s_buffer.AppendLast2Digits(dt.Minute);
            s_buffer.Append(':');
            s_buffer.AppendLast2Digits(dt.Second);
            s_buffer.Append('.');
            s_buffer.AppendLast7Digits(dt.Ticks);
            s_buffer.Append(' ');
        }

        if (_startupTimeEnabled)
        {
            var ts = messageDto.StartupTime();
            var secondsWithFraction = ts.Ticks * 1E-07m;
            s_buffer.Append(secondsWithFraction.ToString(CultureInfo.InvariantCulture));
            s_buffer.Append(' ');
        }

        if (messageDto.ThreadId != s_unityMainThreadId)
        {
            s_buffer.Append("[ThreadId=");
            s_buffer.Append(messageDto.ThreadId.ToString(CultureInfo.InvariantCulture));
            s_buffer.Append("] ");
        }

        s_buffer.Append(messageDto.LoggerName);
        s_buffer.Append(' ');

        s_buffer.Append('[');
        s_buffer.Append(LogLevelExtension.LogToString(messageDto.LogLevel));
        s_buffer.Append(']');

        var prefix = " ";
        if (!string.IsNullOrEmpty(messageDto.Message))
        {
            s_buffer.Append(prefix);
            s_buffer.Append(messageDto.Message);
            prefix = Environment.NewLine;
        }

        if (messageDto.Exception != null)
        {
            s_buffer.Append(prefix);
            s_buffer.Append(messageDto.Exception.ToString());
            prefix = Environment.NewLine;
        }

        if (messageDto.Location != null)
        {
            s_buffer.Append(prefix);
            s_buffer.Append("Location Trace");
            s_buffer.Append(Environment.NewLine);
            s_buffer.Append(GetLocationString(messageDto.Location));
        }

        s_buffer.Append(Environment.NewLine);

        return s_buffer.GetBytes(out bytes);
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