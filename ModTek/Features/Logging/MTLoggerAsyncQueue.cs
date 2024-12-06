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
            var logger = Log.Main.Trace;
            if (logger is null)
            {
                return;
            }

            var dispatchStats = LoggingFeature.DispatchStopWatch.GetStats(); // fetch the overhead introduced by async logging
            var offloadedTime = stats.TotalTime.Subtract(dispatchStats.TotalTime);

            var latencyStats = MTLoggerMessageDto.LatencyStopWatch.GetStats();
            var dtoStats = LoggingFeature.MessageSetupStopWatch.GetStats();
            var filterStats = AppenderFile.FiltersStopWatch.GetStats();
            var formatterStats = AppenderFile.FormatterStopWatch.GetStats();
            var writeStats = AppenderFile.WriteStopwatch.GetStats();

            logger.Log(
                $"""
                Asynchronous logging offloaded {offloadedTime} from the main thread.
                End-to-end processing had an average latency of {latencyStats.AverageNanoseconds / 1_000_000}ms.
                  On-thread processing took a total of {dtoStats.TotalTime} with an average of {dtoStats.AverageNanoseconds}ns.
                    Dispatched {dispatchStats.Count} times, taking a total of {dispatchStats.TotalTime} with an average of {dispatchStats.AverageNanoseconds}ns.
                  Off-thread processing took a total of {stats.TotalTime} with an average of {stats.AverageNanoseconds}ns.
                    Filters took a total of {filterStats.TotalTime} with an average of {filterStats.AverageNanoseconds}ns.
                    Formatter took a total of {formatterStats.TotalTime} with an average of {formatterStats.AverageNanoseconds}ns.
                    Write called {writeStats.Count} times, taking a total of {writeStats.TotalTime} with an average of {writeStats.AverageNanoseconds}ns.
                """
            );
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