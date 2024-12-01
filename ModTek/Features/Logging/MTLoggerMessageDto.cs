using System;
using System.Diagnostics;
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
    internal readonly int ThreadId;
    
    internal MTLoggerMessageDto(
        long timestamp,
        string loggerName,
        LogLevel logLevel,
        string message,
        Exception exception,
        IStackTrace location,
        int threadId
    ) {
        _timestamp = timestamp;
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
        return MTStopwatch.TimeSpanFromTicks(_timestamp - InitialStopwatchTimestamp);
    }

    internal static long GetTimestamp() => Stopwatch.GetTimestamp();
    
    // memory tracking
    internal int EstimatedSizeInMemory => SizeInMemoryEstimatedOverhead + LoggerName.Length + (Message?.Length ?? 0);
    
    private const int SizeInMemoryEstimatedOverhead =
        ReferenceTypeEstimatedOverhead
        + (ConcurrentQueueSegmentEstimatedOverhead + (ItemsPerSegment-1))/ItemsPerSegment // round up
        + sizeof(long) // _timestamp
        + sizeof(long) // LoggerName
        + sizeof(LogLevel)
        + sizeof(long) // Message
        + sizeof(long) // ignoring exception object, rarely used and complex to calculate
        + sizeof(long) // ignoring location object, rarely used and complex to calculate
        + sizeof(int); // ThreadId

    private const int ConcurrentQueueSegmentEstimatedOverhead =
        ReferenceTypeEstimatedOverhead
        + ItemsPerSegment * sizeof(long) // m_array
        + ItemsPerSegment * sizeof(bool) // m_state
        + sizeof(long) // m_next
        + sizeof(long) // m_index
        + sizeof(int) // m_low
        + sizeof(int) // m_high
        + sizeof(long); // m_source

    private const int ItemsPerSegment = 32;
    
    private const int ReferenceTypeEstimatedOverhead = 16;
}