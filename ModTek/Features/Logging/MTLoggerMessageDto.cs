using System;
using System.Diagnostics;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging;

internal struct MTLoggerMessageDto
{
    internal static readonly TimeSpan InitialUnityStartupTime;
    internal static readonly long InitialStopwatchTimestamp;
    internal static readonly DateTimeOffset InitialDatTimeOffsetUtc;

    static MTLoggerMessageDto()
    {
        InitialUnityStartupTime = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
        InitialStopwatchTimestamp = Stopwatch.GetTimestamp();
        InitialDatTimeOffsetUtc = DateTimeOffset.UtcNow;
    }

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
        return InitialUnityStartupTime.Add(GetElapsedSinceInitial());
    }

    internal DateTimeOffset GetDateTimeOffsetUtc()
    {
        return InitialDatTimeOffsetUtc.Add(GetElapsedSinceInitial());
    }

    private TimeSpan GetElapsedSinceInitial()
    {
        return MTStopwatch.TimeSpanFromTicks(Timestamp - InitialStopwatchTimestamp);
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