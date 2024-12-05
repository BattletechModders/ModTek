using System;
using HBS.Logging;

namespace ModTek.Features.Logging;

internal static class LogLevelExtension
{
    internal static string LogToString(LogLevel level)
    {
        var eLogLevel = Convert(level);
        return eLogLevel switch // fast switch with static string, in order of most occuring
        {
            ELogLevels.Trace => "TRACE",
            ELogLevels.Debug => "DEBUG",
            ELogLevels.Log => "LOG",
            ELogLevels.Warning => "WARN",
            ELogLevels.Error => "ERROR",
            ELogLevels.Fatal => "FATAL",
            ELogLevels.OFF => "OFF",
            _ => throw new ArgumentOutOfRangeException()
        };
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