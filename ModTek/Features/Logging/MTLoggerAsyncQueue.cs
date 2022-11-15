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
        _queue = new BlockingCollection<MTLoggerMessageDto>(10 * 1024);
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
        Callback = stats => Log.Main.Debug?.Log($"Asynchronous logging offloaded {stats.TotalMS - _queueStopwatch.GetStats().TotalMS} ms from the main thread."),
        CallbackForEveryNumberOfMeasurements = 1000
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

    private bool CurrentThreadIsLoggerThread() => Thread.CurrentThread == _thread;


    private static readonly MTStopwatch _queueStopwatch = new();
    // return false only if there was an error or async is not wanted
    internal bool Add(MTLoggerMessageDto messageDto)
    {
        _queueStopwatch.Start();
        try
        {
            if (!_queue.IsAddingCompleted && !CurrentThreadIsLoggerThread())
            {
                _queue.Add(messageDto);
                return true;
            }
        }
        catch
        {
            // ignore
        }
        finally
        {
            _queueStopwatch.Stop();
        }
        return false;
    }
}