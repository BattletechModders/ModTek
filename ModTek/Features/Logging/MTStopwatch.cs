using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using ModTek.Util;

namespace ModTek.Features.Logging;

internal class MTStopwatch
{
    internal Action<Stats> Callback;
    internal long CallbackForEveryNumberOfMeasurements = 100;
    internal long SkipFirstNumberOfMeasurements = 10_000; // let's wait for the JIT to warm up

    internal uint Sampling
    {
        set
        {
            _sampling = value;
            _sampleIfRandomSmallerOrEqualsTo = ulong.MaxValue / value;
        }
    }
    private uint _sampling = 1;
    private ulong _sampleIfRandomSmallerOrEqualsTo = ulong.MaxValue;

    private long _ticks;
    private long _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Measurement StartMeasurement()
    {
        return new Measurement(this);
    }
    internal readonly struct Measurement(MTStopwatch stopwatch)
    {
        private readonly long _begin = Stopwatch.GetTimestamp();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Stop()
        {
            stopwatch.EndMeasurement(_begin);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EndMeasurementSampled(long start)
    {
        // measuring takes about 16-30ns
        // fast random is much faster
        if (FastRandom.NextUInt64() <= _sampleIfRandomSmallerOrEqualsTo)
        {
            AddMeasurement(Stopwatch.GetTimestamp() - start);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EndMeasurement(long start)
    {
        AddMeasurement(Stopwatch.GetTimestamp() - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Begin()
        {
            _begin = Stopwatch.GetTimestamp();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal long End()
        {
            return Stopwatch.GetTimestamp() - _begin;
        }
    }

    internal Stats GetStats() => new(this);
    internal readonly struct Stats
    {
        private readonly MTStopwatch _sw;
        internal long Ticks { get; }
        internal long Count { get; }
        internal TimeSpan TotalTime => TimeSpanFromTicks(Ticks);
        internal long AverageNanoseconds => Count <= 0 ? 0 : (long)((double)Ticks / Count / Stopwatch.Frequency * 1e+9);

        internal Stats(MTStopwatch sw)
        {
            Ticks = Interlocked.Read(ref sw._ticks) * sw._sampling;
            Count = Math.Max(Interlocked.Read(ref sw._count) - sw.SkipFirstNumberOfMeasurements, 0) * sw._sampling;
            _sw = sw;
        }

        internal Stats(MTStopwatch sw, long ticks, long count)
        {
            Ticks = ticks * sw._sampling;
            Count = Math.Max(count - sw.SkipFirstNumberOfMeasurements, 0) * sw._sampling;
            _sw = sw;
        }

        public override string ToString()
        {
            var verb = _sw._sampling > 1 ? "estimated at" : "measured";
            return $"{verb} {Count} times, taking a total of {TotalTime} with an average of {AverageNanoseconds}ns";
        }
    }

    internal static TimeSpan TimeSpanFromTicks(long elapsedTicks)
    {
        return Stopwatch.IsHighResolution ? TimeSpan.FromTicks(elapsedTicks / (Stopwatch.Frequency / 10000000L)) : TimeSpan.FromTicks(elapsedTicks);
    }
}