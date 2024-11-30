using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace ModTek.Features.Logging;

internal class MTLoggerAsyncQueue
{
    private readonly Action<MTLoggerMessageDto> _processor;
    private readonly BlockingCollection<MTLoggerMessageDto> _queue;
    private readonly Thread _thread;
    internal MTLoggerAsyncQueue(Action<MTLoggerMessageDto> processor)
    {
        _processor = processor;
        _queue = new BlockingCollection<MTLoggerMessageDto>(100_000);
        Application.quitting += () => _queue.CompleteAdding();
        _thread = new Thread(Loop)
        {
            Name = nameof(MTLoggerAsyncQueue),
            Priority = ThreadPriority.BelowNormal, // game should take priority
            IsBackground = false // don't exit automatically
        };
        _thread.Start();
    }

    private static readonly MTStopwatch _loggingStopwatch = new()
    {
        Callback = stats =>
        {
            var dispatchStats = _dispatchStopWatch.GetStats();
            var offloadedTime = stats.TotalTime.Subtract(dispatchStats.TotalTime);
            Log.Main.Debug?.Log($"Asynchronous logging offloaded {offloadedTime} from the main thread, dispatched {dispatchStats.Count} log statements in {dispatchStats.TotalTime} with an average of {dispatchStats.AverageTime}.");
        },
        CallbackForEveryNumberOfMeasurements = 10_000
    };

    private void Loop()
    {
        try
        {
            while (!_queue.IsCompleted)
            {
                var message = _queue.Take();
                try
                {
                    _loggingStopwatch.Track(() => _processor(message));
                }
                catch (Exception e)
                {
                    LoggingFeature.WriteExceptionToFatalLog(e);
                }
            }
        }
        catch (InvalidOperationException)
        {
            // ignore
        }
        finally
        {
            _queue.Dispose();
        }
    }

    internal bool ThreadIsLoggerThread(Thread thread) => thread == _thread;

    private static readonly MTStopwatch _dispatchStopWatch = new();
    // return false if, for example, the queue was already "completed"
    internal bool Add(MTLoggerMessageDto messageDto)
    {
        _dispatchStopWatch.Start();
        try
        {
            _queue.Add(messageDto);
            return true;
        }
        catch
        {
            // ignore
        }
        finally
        {
            _dispatchStopWatch.Stop();
        }
        return false;
    }
}