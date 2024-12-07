using System;
using System.Diagnostics;
using System.Threading;

namespace ModTek.Features.Logging;

internal class MTStopwatch
{
    internal Action<Stats> Callback { get; set; }
    internal long CallbackForEveryNumberOfMeasurements { get; set; } = 100;

    private long _ticks;
    private long _count;
    private Tracker _tracker;

    internal void Track(Action action)
    {
        Start();
        try
        {
            action();
        }
        finally
        {
            Stop();
        }
    }

    internal void Start()
    {
        _tracker.Begin();
    }

    internal void Stop()
    {
        var elapsed = _tracker.End();
        AddMeasurement(elapsed);
    }

    internal void AddMeasurement(long elapsedTicks)
    {
        var ticks = Interlocked.Add(ref _ticks, elapsedTicks);
        var count = Interlocked.Increment(ref _count);
        if (Callback != null)
        {
            if (count % CallbackForEveryNumberOfMeasurements == 0)
            {
                Callback.Invoke(new Stats(ticks, count));
            }
        }
    }

    internal struct Tracker
    {
        private long _begin;
        internal void Begin()
        {
            _begin = Stopwatch.GetTimestamp();
        }

        internal long End()
        {
            return Stopwatch.GetTimestamp() - _begin;
        }
    }

    internal Stats GetStats() => new(this);
    internal readonly struct Stats
    {
        internal long Ticks { get; }
        internal long Count { get; }
        internal TimeSpan TotalTime => TimeSpanFromTicks(Ticks);
        internal long AverageNanoseconds => Count == 0 ? 0 : (long)((double)Ticks / Count / Stopwatch.Frequency * 1e+9);

        internal Stats(MTStopwatch sw)
        {
            Ticks = Interlocked.Read(ref sw._ticks);
            Count = Interlocked.Read(ref sw._count);
        }

        internal Stats(long ticks, long count)
        {
            Ticks = ticks;
            Count = count;
        }
    }

    internal static TimeSpan TimeSpanFromTicks(long elapsedTicks)
    {
        return Stopwatch.IsHighResolution ? TimeSpan.FromTicks(elapsedTicks / (Stopwatch.Frequency / 10000000L)) : TimeSpan.FromTicks(elapsedTicks);
    }
}