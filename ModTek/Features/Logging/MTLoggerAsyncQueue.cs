using System;
using System.Threading;
using ModTek.Util.Stopwatch;
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
            Priority = ThreadPriority.AboveNormal,
            IsBackground = false // don't exit automatically
        };
        thread.Start();
        LogWriterThreadId = thread.ManagedThreadId;
    }

    private static readonly MTStopwatchWithCallback s_loggingStopwatch = new(stats =>
        {
            var logger = Log.Main.Trace;
            if (logger is null)
            {
                return;
            }

            var dispatchStats = LoggingFeature.DispatchStopWatch.GetStats();
            var offloadedTime = stats.TotalTime.Subtract(dispatchStats.TotalTime);
            var latencyStats = MTLoggerMessageDto.LatencyStopWatch.GetStats();

            logger.Log(
                $"""
                Asynchronous logging offloaded at least {offloadedTime} from the main thread.
                Async internal processing had an average latency of {latencyStats.AverageNanoseconds / 1_000}us.
                  On-thread processing {dispatchStats}.
                  Off-thread processing took a total of {stats.TotalTime} with an average of {stats.AverageNanoseconds}ns.
                    Flushing (to disk) {AppenderFile.FlushStopWatch.GetStats()}.
                    Filters {AppenderFile.FiltersStopWatch.GetStats()}.
                    Formatter {AppenderFile.FormatterStopWatch.GetStats()}.
                      UTF8-Fallback {FastBuffer.UTF8FallbackStopwatch.GetStats()}.
                    Write (to OS buffers) {AppenderFile.WriteStopwatch.GetStats()}.
                """
            );
        }
    );

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

                var measurement = MTStopwatch.GetTimestamp();
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
                    s_loggingStopwatch.EndMeasurement(measurement);
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