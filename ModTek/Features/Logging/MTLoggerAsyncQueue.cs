using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace ModTek.Features.Logging
{
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

        private readonly MTStopwatch _stopwatch = new MTStopwatch
        {
            Callback = stats => MTLogger.Debug.Log($"Asynchronous logging offloaded {stats.TotalMS} ms from the main thread."),
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
                        _stopwatch.Track(() => _processor(message));
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            finally
            {
                _queue.Dispose();
            }
        }

        private bool CurrentThreadIsLoggerThread() => Thread.CurrentThread == _thread;

        private MTStopwatch.Tracker _queueTracker;
        // return false only if there was an error or async is not wanted
        internal bool Add(MTLoggerMessageDto messageDto)
        {
            _queueTracker.Begin();
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
                // this removes the wait times
                _stopwatch.AddMeasurement(-_queueTracker.End(), 0);
            }
            return false;
        }
    }
}
