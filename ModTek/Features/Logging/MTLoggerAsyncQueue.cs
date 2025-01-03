using System;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace ModTek.Features.Logging;

internal class MTLoggerAsyncQueue
{
    private readonly LightWeightBlockingQueue _queue;
    internal readonly int LogWriterThreadId;

    internal MTLoggerAsyncQueue()
    {
        _queue = new LightWeightBlockingQueue();
        Application.quitting += () => _queue._shuttingOrShutDown = true;
        var thread = new Thread(LoggingLoop)
        {
            Name = nameof(MTLoggerAsyncQueue),
            Priority = ThreadPriority.BelowNormal, // game should take priority
            IsBackground = false // don't exit automatically
        };
        thread.Start();
        LogWriterThreadId = thread.ManagedThreadId;
    }

    private static readonly MTStopwatch s_loggingStopwatch = new()
    {
        Callback = stats =>
        {
            var logger = Log.Main.Trace;
            if (logger is null)
            {
                return;
            }

            var dispatchStats = LoggingFeature.DispatchStopWatch.GetStats(); // fetch the overhead introduced by async logging
            var offloadedTime = stats.TotalTime.Subtract(dispatchStats.TotalTime);

            var latencyStats = MTLoggerMessageDto.LatencyStopWatch.GetStats();
            var dtoStats = LoggingFeature.MessageSetupStopWatch.GetStats();

            logger.Log(
                $"""
                Asynchronous logging offloaded {offloadedTime} from the main thread.
                Flushed {AppenderFile.FlushStopWatch.GetStats()}.
                End-to-end processing had an average latency of {latencyStats.AverageNanoseconds / 1_000_000}ms.
                  On-thread processing took a total of {dtoStats.TotalTime} with an average of {dtoStats.AverageNanoseconds}ns.
                    Dispatch {dispatchStats}.
                  Off-thread processing took a total of {stats.TotalTime} with an average of {stats.AverageNanoseconds}ns.
                    Filters {AppenderFile.FiltersStopWatch.GetStats()}.
                    Formatter {AppenderFile.FormatterStopWatch.GetStats()}.
                      UTF8-Fallback {FastBuffer.UTF8FallbackStopwatch.GetStats()}.
                    Write {AppenderFile.WriteStopwatch.GetStats()}.
                """
            );
        },
        CallbackForEveryNumberOfMeasurements = 50_000
    };

    public bool IsShuttingOrShutDown => _queue._shuttingOrShutDown;

    public void WaitForShutdown()
    {
        var spinWait = new SpinWait();
        while (true)
        {
            if (_isShutdown)
            {
                break;
            }
            spinWait.SpinOnce();
        }
    }

    private volatile bool _isShutdown;
    private void LoggingLoop()
    {
        try
        {
            while (true)
            {
                ref var message = ref _queue.AcquireCommittedOrWait();

                var measurement = s_loggingStopwatch.BeginMeasurement();
                try
                {
                    LoggingFeature.LogMessage(ref message);
                }
                catch (Exception e)
                {
                    LoggingFeature.WriteExceptionToFatalLog(e);
                }
                finally
                {
                    message.Reset();
                    measurement.End();
                }
            }
        }
        catch (LightWeightBlockingQueue.ShutdownException)
        {
            _isShutdown = true;
        }
    }

    internal ref MTLoggerMessageDto AcquireUncommitedOrWait()
    {
        return ref _queue.AcquireUncommitedOrWait();
    }
}