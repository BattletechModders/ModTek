using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging;

internal struct MTLoggerMessageDto
{
    private static readonly long s_stopwatchTimestamp = Stopwatch.GetTimestamp();
    private static readonly DateTime s_dateTime = DateTime.UtcNow;
    private static readonly TimeSpan s_unityStartupTime = TimeSpan.FromSeconds(Time.realtimeSinceStartup);

    internal static void GetTimings(out long stopwatchTimestamp, out DateTime dateTime, out TimeSpan unityStartupTime)
    {
        stopwatchTimestamp = s_stopwatchTimestamp;
        dateTime = s_dateTime;
        unityStartupTime = s_unityStartupTime;
    }

    internal volatile bool CommittedToQueue;
    internal int QueueSizeAtTimeOfDequeue;
    internal bool HasMore => QueueSizeAtTimeOfDequeue > 1;

    // either these are set
    internal long Timestamp;
    internal string LoggerName;
    internal LogLevel LogLevel;
    internal string Message;
    internal Exception Exception;
    internal IStackTrace Location;
    internal int ThreadId;
    // or this is set
    internal bool FlushToDisk => FlushToDiskPostEvent != null;
    internal ManualResetEventSlim FlushToDiskPostEvent;
    
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

    // allow queue to read it
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Commit()
    {
        this.CommittedToQueue = true;
    }

    internal static readonly MTStopwatch LatencyStopWatch = new();
    // uncommit, prepare for re-use
    internal void Reset()
    {
        LatencyStopWatch.AddMeasurement(Stopwatch.GetTimestamp() - Timestamp);
        this = default;
    }
}