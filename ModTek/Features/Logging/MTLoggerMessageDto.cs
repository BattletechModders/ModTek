using System;
using System.Diagnostics;
using System.Threading;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging;

internal class MTLoggerMessageDto
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

    private readonly long _timestamp;
    internal readonly string LoggerName;
    internal readonly LogLevel LogLevel;
    internal readonly string Message;
    internal readonly Exception Exception;
    internal readonly IStackTrace Location;
    internal readonly Thread NonMainThread;
    
    internal MTLoggerMessageDto(
        long timestamp,
        string loggerName,
        LogLevel logLevel,
        string message,
        Exception exception,
        IStackTrace location,
        Thread nonMainThread
    ) {
        _timestamp = timestamp;
        LoggerName = loggerName;
        LogLevel = logLevel;
        Message = message;
        Exception = exception;
        Location = location;
        NonMainThread = nonMainThread;
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
        return MTStopwatch.TimeSpanFromTicks(_timestamp - InitialStopwatchTimestamp);
    }

    internal static long GetTimestamp() => Stopwatch.GetTimestamp();
}