using System;
using System.Diagnostics;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging;

internal struct MTLoggerMessageDto
{
    private static long s_stopwatchTimestamp = Stopwatch.GetTimestamp();
    private static DateTime s_dateTime = DateTime.UtcNow;
    private static TimeSpan s_unityStartupTime = TimeSpan.FromSeconds(Time.realtimeSinceStartup);

    internal volatile bool CommittedToQueue;
    internal long Timestamp;
    internal string LoggerName;
    internal LogLevel LogLevel;
    internal string Message;
    internal Exception Exception;
    internal IStackTrace Location;
    internal int ThreadId;

    public MTLoggerMessageDto()
    {
    }
    
    internal MTLoggerMessageDto(
        long timestamp,
        string loggerName,
        LogLevel logLevel,
        string message,
        Exception exception,
        IStackTrace location,
        int threadId
    ) {
        Timestamp = timestamp;
        LoggerName = loggerName;
        LogLevel = logLevel;
        Message = message;
        Exception = exception;
        Location = location;
        ThreadId = threadId;
    }
    
    internal TimeSpan StartupTime()
    {
        return s_unityStartupTime.Add(GetElapsedSinceInitial());
    }

    internal DateTime GetDateTime()
    {
        return s_dateTime.Add(GetElapsedSinceInitial());
    }

    private TimeSpan GetElapsedSinceInitial()
    {
        return MTStopwatch.TimeSpanFromTicks(Timestamp - s_stopwatchTimestamp);
    }

    internal static long GetTimestamp() => Stopwatch.GetTimestamp();

    // allow queue to read it
    internal void Commit()
    {
        this.CommittedToQueue = true;
    }

    // uncommit, prepare for re-use
    internal void Reset()
    {
        this = default;
    }
}