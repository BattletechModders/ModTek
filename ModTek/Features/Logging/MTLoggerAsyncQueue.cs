using System;
using System.Text;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace ModTek.Features.Logging;

internal class MTLoggerAsyncQueue
{
    private readonly Action<MTLoggerMessageDto> _processor;
    private readonly LightWeightBlockingQueue _queue;
    internal readonly int LogWriterThreadId;
    
    internal MTLoggerAsyncQueue(Action<MTLoggerMessageDto> processor)
    {
        _processor = processor;
        _queue = new LightWeightBlockingQueue();
        Application.quitting += () => _queue.Shutdown();
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
            var debug = Log.Main.Debug;
            if (debug is not null)
            {
                var dispatchStats = LoggingFeature.DispatchStopWatch.GetStats(); // fetch the overhead introduced by async logging
                var offloadedTime = stats.TotalTime.Subtract(dispatchStats.TotalTime);
                debug.Log($"Asynchronous logging offloaded {offloadedTime} from the main thread.");

                var trace = Log.Main.Trace;
                if (trace is not null)
                {
                    var sb = new StringBuilder();

                    var dtoStats = LoggingFeature.MessageDtoStopWatch.GetStats();
                    sb.Append($"\nDTO setup took a total of {dtoStats.TotalTime} with an average of {dtoStats.AverageNanoseconds}ns.");

                    sb.Append($"\nDispatched {dispatchStats.Count} times, taking a total of {dispatchStats.TotalTime} with an average of {dispatchStats.AverageNanoseconds}ns.");

                    var filterStats = AppenderFile.FiltersStopWatch.GetStats();
                    sb.Append($"\nFilters took a total of {filterStats.TotalTime} with an average of {filterStats.AverageNanoseconds}ns.");

                    var formatterStats = AppenderFile.FormatterStopWatch.GetStats();
                    sb.Append($"\nFormatter took a total of {formatterStats.TotalTime} with an average of {formatterStats.AverageNanoseconds}ns.");

                    var bytesStats = AppenderFile.GetBytesStopwatch.GetStats();
                    sb.Append($"\nGetBytes took a total of {bytesStats.TotalTime} with an average of {bytesStats.AverageNanoseconds}ns.");

                    var writeStats = AppenderFile.WriteStopwatch.GetStats();
                    sb.Append($"\nWrite called {writeStats.Count} times, taking a total of {writeStats.TotalTime} with an average of {writeStats.AverageNanoseconds}ns.");

                    trace.Log(sb.ToString());
                }
            }
        },
        CallbackForEveryNumberOfMeasurements = 50_000
    };

    private void LoggingLoop()
    {
        while (true)
        {
            ref var message = ref _queue.AcquireCommittedOrWait();

            s_loggingStopwatch.Start();
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
                message.Reset();
                s_loggingStopwatch.Stop();
            }
        }
    }

    internal ref MTLoggerMessageDto AcquireUncommitedOrWait()
    {
        return ref _queue.AcquireUncommitedOrWait();
    }
}