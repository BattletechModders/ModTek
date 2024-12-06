using System;
using System.Text;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace ModTek.Features.Logging;

internal class MTLoggerAsyncQueue
{
    private readonly IMessageProcessor _processor;
    private readonly LightWeightBlockingQueue _queue;
    internal readonly int LogWriterThreadId;

    internal interface IMessageProcessor
    {
        void Process(ref MTLoggerMessageDto message);
    }
    
    internal MTLoggerAsyncQueue(IMessageProcessor processor)
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

                    var latencyStats = MTLoggerMessageDto.LatencyStopWatch.GetStats();
                    sb.Append($"\nEnd-to-end processing had a latency average of {latencyStats.AverageNanoseconds / 1_000_000}ms.");

                    var dtoStats = LoggingFeature.MessageSetupStopWatch.GetStats();
                    sb.Append($"\n  On-thread processing took a total of {dtoStats.TotalTime} with an average of {dtoStats.AverageNanoseconds}ns.");

                    sb.Append($"\n    Dispatched {dispatchStats.Count} times, taking a total of {dispatchStats.TotalTime} with an average of {dispatchStats.AverageNanoseconds}ns.");

                    sb.Append($"\n  Off-thread processing took a total of {stats.TotalTime} with an average of {stats.AverageNanoseconds}ns.");

                    var filterStats = AppenderFile.FiltersStopWatch.GetStats();
                    sb.Append($"\n    Filters took a total of {filterStats.TotalTime} with an average of {filterStats.AverageNanoseconds}ns.");

                    var formatterStats = AppenderFile.FormatterStopWatch.GetStats();
                    sb.Append($"\n    Formatter took a total of {formatterStats.TotalTime} with an average of {formatterStats.AverageNanoseconds}ns.");

                    var writeStats = AppenderFile.WriteStopwatch.GetStats();
                    sb.Append($"\n    Write called {writeStats.Count} times, taking a total of {writeStats.TotalTime} with an average of {writeStats.AverageNanoseconds}ns.");

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
                _processor.Process(ref message);
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