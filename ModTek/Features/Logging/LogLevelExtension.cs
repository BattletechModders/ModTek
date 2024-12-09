using System;
using System.Diagnostics;
using HBS.Logging;

namespace ModTek.Features.Logging;

internal static class LogLevelExtension
{
    internal static string LogToString(LogLevel level)
    {
        var eLogLevel = ConvertHbsLogLevelToELogLevel(level);
        return eLogLevel switch // fast switch with static string, in order of most occuring
        {
            ELogLevels.Trace => "TRACE",
            ELogLevels.Debug => "DEBUG",
            ELogLevels.Log => "LOG",
            ELogLevels.Warning => "WARNING",
            ELogLevels.Error => "ERROR",
            ELogLevels.Fatal => "FATAL",
            ELogLevels.OFF => "OFF",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    internal static bool IsLogLevelGreaterThan(LogLevel loggerLevel, LogLevel messageLevel)
    {
        return ConvertHbsLogLevelToELogLevel(messageLevel) >= ConvertHbsLogLevelToELogLevel(loggerLevel);
    }

    private static ELogLevels ConvertHbsLogLevelToELogLevel(LogLevel level)
    {
        var intLevel = (int)level;
        if (intLevel is >= (int)LogLevel.Debug and <= (int)LogLevel.Error)
        {
            intLevel = intLevel * 10 + (int)ELogLevels.Debug;
        }
        return (ELogLevels)intLevel;
    }

    internal static TraceLevel GetLevelFilter(ILog log)
    {
        if (log is not Logger.LogImpl logImpl)
        {
            return TraceLevel.Verbose; // either off or verbose, better too much
        }
        var effectiveLevel = logImpl.EffectiveLevel;
        var eLogLevel = ConvertHbsLogLevelToELogLevel(effectiveLevel);
        return ConvertELogLevelToTraceLevel(eLogLevel);
    }

    private static TraceLevel ConvertELogLevelToTraceLevel(ELogLevels level)
    {
        level = (ELogLevels)((int)level / 10 * 10);
        return level switch
        {
            ELogLevels.OFF => TraceLevel.Off,
            ELogLevels.Fatal => TraceLevel.Error,
            ELogLevels.Error => TraceLevel.Error,
            ELogLevels.Warning => TraceLevel.Warning,
            ELogLevels.Log => TraceLevel.Info,
            ELogLevels.Debug => TraceLevel.Verbose,
            ELogLevels.Trace => TraceLevel.Verbose,
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
        };
    }

    // log levels
    private enum ELogLevels
    {
        Trace = 200, // (extended) trace steps and variables, usually slows down the game considerably
        Debug = 210, // also used to trace steps and variables, but at a reduced rate to keep the logfiles readable and performance ok
        Log = 220, // minimal logs required to know what the user is doing in general, should have no impact on performance; user clicked x
        Warning = 230, // something wrong happened, but the mod will deal with it; a wrong config value, which has a safe fallback
        Error = 240, // something wrong happened, the mod might not work correctly going forward; an optional patch that didn't apply
        Fatal = 250, // (extended) something wrong happened, the the game is not usable anymore and needs to be shut down; missing basic settings
        OFF = 300 // (extended) useful to know how to disable logging for good
    }
}