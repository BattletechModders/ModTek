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

    [ThreadStatic]
    private static FastBuffer s_buffer;

    internal int GetFormattedLogLine(ref MTLoggerMessageDto messageDto, out byte[] threadUnsafeBytes)
    {
        s_buffer ??= new FastBuffer();
        s_buffer.Reset();
        s_buffer.Pin();

        if (_absoluteTimeEnabled)
        {
            var dt = messageDto.GetDateTime();
            if (!_absoluteTimeUseUtc)
            {
                dt = dt.ToLocalTime();
            }
            s_buffer.Append(dt);
        }

        if (_startupTimeEnabled)
        {
            var ts = messageDto.StartupTime();
            s_buffer.Append(ts);
        }

        if (messageDto.ThreadId != s_unityMainThreadId)
        {
            s_buffer.Append(s_threadIdPrefix);
            s_buffer.Append(messageDto.ThreadId);
            s_buffer.Append(s_threadIdSuffix);
        }

        // TODO create injector and add a nameAsBytes field that should be passed along instead of string
        s_buffer.Append(messageDto.LoggerName);

        s_buffer.Append(LogLevelExtension.GetCachedFormattedBytes(messageDto.LogLevel));

        var prefix = s_whitespaceBytes;
        if (!string.IsNullOrEmpty(messageDto.Message))
        {
            s_buffer.Append(prefix);
            s_buffer.Append(messageDto.Message);
            prefix = s_environmentNewline;
        }

        if (messageDto.Exception != null)
        {
            s_buffer.Append(prefix);
            s_buffer.Append(messageDto.Exception.ToString());
            prefix = s_environmentNewline;
        }

        if (messageDto.Location != null)
        {
            s_buffer.Append(prefix);
            s_buffer.Append(s_locationTraceLabel);
            s_buffer.Append(s_environmentNewline);
            s_buffer.Append(GetLocationString(messageDto.Location));
        }

        s_buffer.Append(s_environmentNewline);

        s_buffer.Unpin();

        return s_buffer.GetThreadUnsafeBytes(out threadUnsafeBytes);
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
    // switch to inlined " "u8 once https://github.com/MonoMod/MonoMod/issues/194 is fixed
    private static readonly byte[] s_threadIdPrefix = Encoding.UTF8.GetBytes("[ThreadId=");
    private static readonly byte[] s_threadIdSuffix = Encoding.UTF8.GetBytes("] ");
    private static readonly byte[] s_whitespaceBytes = Encoding.UTF8.GetBytes(" ");
    private static readonly byte[] s_environmentNewline = Encoding.UTF8.GetBytes(Environment.NewLine);
    private static readonly byte[] s_locationTraceLabel = Encoding.UTF8.GetBytes("Location Trace");
}