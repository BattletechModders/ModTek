using System;
using System.Text;
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

    internal void SerializeMessage(ref MTLoggerMessageDto messageDto, FastBuffer buffer)
    {
        if (_absoluteTimeEnabled)
        {
            var dt = messageDto.GetDateTime();
            if (!_absoluteTimeUseUtc)
            {
                dt = dt.ToLocalTime();
            }
            buffer.Append(dt);
        }

        if (_startupTimeEnabled)
        {
            var ts = messageDto.StartupTime();
            buffer.Append(ts);
        }

        if (messageDto.ThreadId != s_unityMainThreadId)
        {
            buffer.Append(s_threadIdPrefix);
            buffer.Append(messageDto.ThreadId);
            buffer.Append(s_threadIdSuffix);
        }

        buffer.Append(messageDto.LoggerName);

        buffer.Append(LogLevelExtension.GetFormattedBytes(messageDto.LogLevel));

        var prefix = s_whitespaceBytes;
        if (!string.IsNullOrEmpty(messageDto.Message))
        {
            buffer.Append(prefix);
            buffer.Append(messageDto.Message);
            prefix = s_environmentNewline;
        }

        if (messageDto.Exception != null)
        {
            buffer.Append(prefix);
            buffer.Append(messageDto.Exception.ToString());
            prefix = s_environmentNewline;
        }

        if (messageDto.Location != null)
        {
            buffer.Append(prefix);
            buffer.Append(s_locationTraceLabel);
            buffer.Append(s_environmentNewline);
            buffer.Append(GetLocationString(messageDto.Location));
        }

        buffer.Append(s_environmentNewline);
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

    // avoid heap allocations during logging
    // " "u8 using mscorlib ReadOnlySpans is 10x slower than pre-allocating byte arrays and using Buffer.BlockCopy
    private static readonly byte[] s_threadIdPrefix = Encoding.UTF8.GetBytes("[ThreadId=");
    private static readonly byte[] s_threadIdSuffix = Encoding.UTF8.GetBytes("] ");
    private static readonly byte[] s_whitespaceBytes = Encoding.UTF8.GetBytes(" ");
    private static readonly byte[] s_environmentNewline = Encoding.UTF8.GetBytes(Environment.NewLine);
    private static readonly byte[] s_locationTraceLabel = Encoding.UTF8.GetBytes("Location Trace");
}