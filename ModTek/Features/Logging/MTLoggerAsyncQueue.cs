using System;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace ModTek.Features.Logging;

internal class MTLoggerAsyncQueue
{
    private readonly Action<MTLoggerMessageDto> _processor;
    private readonly LightWeightBlockingQueue<MTLoggerMessageDto> _queue;
    internal readonly int LogWriterThreadId;
    
    internal MTLoggerAsyncQueue(Action<MTLoggerMessageDto> processor)
    {
        _processor = processor;
        _queue = new LightWeightBlockingQueue<MTLoggerMessageDto>();
        Application.quitting += () => _queue.CompleteAdding();
        var thread = new Thread(LoggingLoop)
        {
            Name = nameof(MTLoggerAsyncQueue),
            Priority = ThreadPriority.BelowNormal, // game should take priority
            IsBackground = false // don't exit automatically
        };
        thread.Start();
        LogWriterThreadId = thread.ManagedThreadId;
    }

    private static readonly MTStopwatch _loggingStopwatch = new()
    {
        Callback = stats =>
        {
            var dispatchStats = _dispatchStopWatch.GetStats(); // fetch the overhead introduced by async logging
            var offloadedTime = stats.TotalTime.Subtract(dispatchStats.TotalTime);
            Log.Main.Debug?.Log($"Asynchronous logging offloaded {offloadedTime} from the main thread.");

            var trace = Log.Main.Trace;
            if (trace is not null)
            {
                var dtoStats = LoggingFeature.MessageDtoStopWatch.GetStats();
                trace.Log($"DTO setup took a total of {dtoStats.TotalTime} with an average of {dtoStats.AverageNanoseconds}ns.");

                trace.Log($"Dispatched {dispatchStats.Count} times, taking a total of {dispatchStats.TotalTime} with an average of {dispatchStats.AverageNanoseconds}ns.");

                var filterStats = AppenderFile.FiltersStopWatch.GetStats();
                trace.Log($"Filters took at total of {filterStats.TotalTime} with an average of {filterStats.AverageNanoseconds}ns.");

                var formatterStats = AppenderFile.FormatterStopWatch.GetStats();
                trace.Log($"Formatter took a total of {formatterStats.TotalTime} with an average of {formatterStats.AverageNanoseconds}ns.");

                var bytesStats = AppenderFile.GetBytesStopwatch.GetStats();
                trace.Log($"GetBytes took a total of {bytesStats.TotalTime} with an average of {bytesStats.AverageNanoseconds}ns.");

                var writeStats = AppenderFile.WriteStopwatch.GetStats();
                trace.Log($"Write called {writeStats.Count} times, taking a total of {writeStats.TotalTime} with an average of {writeStats.AverageNanoseconds}ns.");

#if MEMORY_TRACE
                trace.Log($"An estimated maximum of {s_memoryEstimatedUsageMax / 1_000_000} MB was ever used by {s_memoryObjectCountMax})
#endif
            }
        },
        CallbackForEveryNumberOfMeasurements = 50_000
    };

    private void LoggingLoop()
    {
        while (true)
        {
            if (!_queue.TryDequeueOrWait(out var message))
            {
                return;
            }
            
            _loggingStopwatch.Start();
            try
            {
                _processor(message);
            }
            catch (Exception e)
            {
                LoggingFeature.WriteExceptionToFatalLog(e);
            }
            finally
            {
                _loggingStopwatch.Stop();
#if MEMORY_TRACE
                UnTrackMemory(message);
#endif
            }
        }
    }

    // tracks all overhead on the main thread that happens due to async logging
    private static readonly MTStopwatch _dispatchStopWatch = new();
    // return false if, for example, the queue was already "completed"
    internal bool Add(MTLoggerMessageDto messageDto)
    {
        _dispatchStopWatch.Start();
        try
        {
            if (_queue.TryEnqueueOrWait(messageDto))
            {
#if MEMORY_TRACE
                TrackMemory(messageDto);
#endif
                return true;
            }
            return false;
        }
        finally
        {
            _dispatchStopWatch.Stop();
        }
    }
    
#if MEMORY_TRACE
    // memory tracking
    private static long s_memoryEstimatedUsage;
    private static long s_memoryEstimatedUsageMax;
    private static long s_memoryObjectCount;
    private static long s_memoryObjectCountMax;
    private static void TrackMemory(MTLoggerMessageDto messageDto)
    {
        var currentCount = Interlocked.Increment(ref s_memoryObjectCount);
        var currentMemoryUse = Interlocked.Add(ref s_memoryEstimatedUsage, messageDto.EstimatedSizeInMemory);

        var knownMax = Interlocked.Read(ref s_memoryEstimatedUsageMax);
        if (knownMax >= currentMemoryUse)
        {
            return;
        }

        while (true)
        {
            var setMax = Interlocked.CompareExchange(ref s_memoryEstimatedUsageMax, currentMemoryUse, knownMax);
            if (setMax == knownMax) // our attempt did set a new max
            {
                // no CAS here, as we don't care if slightly wrong stats are saved
                Volatile.Write(ref s_memoryObjectCountMax, currentCount);
                break;
            }
            if (setMax >= currentMemoryUse) // another thread already set a higher max than we estimated
            {
                break;
            }
            knownMax = setMax; // let's retry
        }
    }
    private static void UnTrackMemory(MTLoggerMessageDto messageDto)
    {
        Interlocked.Add(ref s_memoryEstimatedUsage, -messageDto.EstimatedSizeInMemory);
        Interlocked.Decrement(ref s_memoryObjectCount);
    }
#endif
}