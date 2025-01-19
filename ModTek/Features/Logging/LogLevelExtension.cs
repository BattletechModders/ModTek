using System;
using System.Text;
using HBS.Logging;

namespace ModTek.Features.Logging;

internal static class LogLevelExtension
{
    private static readonly byte[] s_trace = FormatAsBytes(ELogLevels.Trace);
    private static readonly byte[] s_debug = FormatAsBytes(ELogLevels.Debug);
    private static readonly byte[] s_log = FormatAsBytes(ELogLevels.Log);
    private static readonly byte[] s_warning = FormatAsBytes(ELogLevels.Warning);
    private static readonly byte[] s_error = FormatAsBytes(ELogLevels.Error);
    private static readonly byte[] s_fatal = FormatAsBytes(ELogLevels.Fatal);
    private static readonly byte[] s_off = FormatAsBytes(ELogLevels.OFF);
    private static byte[] FormatAsBytes(ELogLevels level) => Encoding.UTF8.GetBytes(" [" + ELogToString(level) + "]");
    // avoid allocations during logging
    internal static byte[] GetFormattedBytes(LogLevel level)
    {
        var eLogLevel = Convert(level);
        return eLogLevel switch // fast switch with static string, in order of most occuring
        {
            ELogLevels.Trace => s_trace,
            ELogLevels.Debug => s_debug,
            ELogLevels.Log => s_log,
            ELogLevels.Warning => s_warning,
            ELogLevels.Error => s_error,
            ELogLevels.Fatal => s_fatal,
            ELogLevels.OFF => s_off,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    internal static bool TryParse(string text, out LogLevel logLevel)
    {
        if ("TRACE".Equals(text, StringComparison.OrdinalIgnoreCase) )
        {
            logLevel = (LogLevel)TraceLogLevel;
            return true;
        }

        if (Enum.TryParse(text, true, out logLevel))
        {
            return true;
        }

        return false;
    }

    internal static string LogToString(LogLevel level)
    {
        var eLogLevel = Convert(level);
        return ELogToString(eLogLevel);
    }
    private static string ELogToString(ELogLevels eLogLevel)
    {
        return eLogLevel.ToString().ToUpperInvariant();
    }

    internal static bool IsLogLevelGreaterThan(LogLevel loggerLevel, LogLevel messageLevel)
    {
        return Convert(messageLevel) >= Convert(loggerLevel);
    }

    private static ELogLevels Convert(LogLevel level)
    {
        var intLevel = (int)level;
        if (intLevel is >= (int)LogLevel.Debug and <= (int)LogLevel.Error)
        {
            intLevel = intLevel * 10 + (int)ELogLevels.Debug;
        }
        return (ELogLevels)intLevel;
    }

    internal const int TraceLogLevel = 200;
    // log levels
    private enum ELogLevels
    {
        Trace = TraceLogLevel, // (extended) trace steps and variables, usually slows down the game considerably
        Debug = 210, // also used to trace steps and variables, but at a reduced rate to keep the logfiles readable and performance ok
        Log = 220, // minimal logs required to know what the user is doing in general, should have no impact on performance; user clicked x
        Warning = 230, // something wrong happened, but the mod will deal with it; a wrong config value, which has a safe fallback
        Error = 240, // something wrong happened, the mod might not work correctly going forward; an optional patch that didn't apply
        Fatal = 250, // (extended) something wrong happened, the the game is not usable anymore and needs to be shut down; missing basic settings
        OFF = 300 // (extended) useful to know how to disable logging for good
    }
}