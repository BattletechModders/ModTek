using System;
using System.Diagnostics;
using System.Threading;

namespace ModTek.Features.Logging;

internal class MTStopwatch
{
    internal Action<Stats> Callback { get; set; }
    internal long CallbackForEveryNumberOfMeasurements { get; set; } = 100;
    internal long SkipFirstNumberOfMeasurements { get; set; } = 1000; // let's wait for the JIT to warm up

    private long _ticks;
    private long _count;

    internal Measurement StartMeasurement()
    {
        return new Measurement(this);
    }
    internal readonly struct Measurement(MTStopwatch stopwatch)
    {
        private readonly long _begin = Stopwatch.GetTimestamp();
        internal void Stop()
        {
            var elapsed = Stopwatch.GetTimestamp() - _begin;
            stopwatch.AddMeasurement(elapsed);
        }
    }

    internal void AddMeasurement(long elapsedTicks)
    {
        var count = Interlocked.Increment(ref _count);
        if (count <= SkipFirstNumberOfMeasurements)
        {
            return;
        }
        var ticks = Interlocked.Add(ref _ticks, elapsedTicks);
        if (Callback != null)
        {
            if (count % CallbackForEveryNumberOfMeasurements == 0)
            {
                Callback.Invoke(new Stats(this, ticks, count));
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
        internal long AverageNanoseconds => Count <= 0 ? 0 : (long)((double)Ticks / Count / Stopwatch.Frequency * 1e+9);

        internal Stats(MTStopwatch sw)
        {
            Ticks = Interlocked.Read(ref sw._ticks);
            Count = Math.Max(Interlocked.Read(ref sw._count) - sw.SkipFirstNumberOfMeasurements, 0);
        }

        internal Stats(MTStopwatch sw, long ticks, long count)
        {
            Ticks = ticks;
            Count = Math.Max(count - sw.SkipFirstNumberOfMeasurements, 0);
        }

        public override string ToString()
        {
            return $"measured {Count} times, taking a total of {TotalTime} with an average of {AverageNanoseconds}ns";
        }
    }

    internal static TimeSpan TimeSpanFromTicks(long elapsedTicks)
    {
        return Stopwatch.IsHighResolution ? TimeSpan.FromTicks(elapsedTicks / (Stopwatch.Frequency / 10000000L)) : TimeSpan.FromTicks(elapsedTicks);
    }
}