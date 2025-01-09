using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ModTek.Util.Stopwatch;

internal class MTStopwatch
{
    protected long _count;
    protected long _ticks;

    internal double _overheadInMeasurement = s_timestampOverhead;

    internal byte TimestampCountPerMeasurement { get; init; } = 2;
    protected double OverheadPerMeasurement => s_timestampOverhead * TimestampCountPerMeasurement;
    internal static double OverheadPerTimestampInNanoseconds => s_timestampOverhead * TicksToNsMultiplier;

    protected static readonly double s_timestampOverhead;
    static MTStopwatch()
    {
        s_timestampOverhead = GetTimestampOverhead();
    }

    private static double GetTimestampOverhead()
    {
        var overhead = 0d;
        for (var r = 0; r < 100; r++) {
            var start = System.Diagnostics.Stopwatch.GetTimestamp();
            const int Count = 1000;
            for (var l = 0; l < Count; l++)
            {
                System.Diagnostics.Stopwatch.GetTimestamp();
            }
            var end = System.Diagnostics.Stopwatch.GetTimestamp();
            overhead = (end - start) / (double)Count;
        }
        return overhead;
    }

    internal void Reset()
    {
        Volatile.Write(ref _count, 0);
        Volatile.Write(ref _ticks, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTimestamp()
    {
        return System.Diagnostics.Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal virtual void EndMeasurement(long start, long delta = 1)
    {
        AddMeasurement(GetTimestamp() - start, delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void AddMeasurement(long elapsedTicks, long delta)
    {
        Interlocked.Add(ref _count, delta);
        Interlocked.Add(ref _ticks, elapsedTicks);
    }

    internal MTStopwatchStats GetStats() => new(this, Volatile.Read(ref _count), Volatile.Read(ref _ticks));

    internal static long TicksMin(long[] ticks)
    {
        var minTick = long.MaxValue;
        for (var i = 0; i < ticks.Length; i++)
        {
            var tick = ticks[i];
            if (tick == 0)
            {
                return 0;
            }

            if (tick < minTick)
            {
                minTick = tick;
            }
        }
        return minTick;
    }
    internal static double TicksAvg(long[] ticks, double ignoreLower, double ignoreUpper)
    {
        Array.Sort(ticks);
        var sum = 0L;
        var start = (int)(ticks.Length * ignoreLower);
        var end = (int)(ticks.Length * (1 - ignoreUpper));
        for (var i = start; i < end; i++)
        {
            sum += ticks[i];
        }
        return sum / (double)(end - start);
    }

    internal static TimeSpan TimeSpanFromTicks(long elapsedTicks)
    {
        return System.Diagnostics.Stopwatch.IsHighResolution ? TimeSpan.FromTicks((long)(elapsedTicks * s_stopWatchTicksToTimeSpanTicksMultiplier)) : TimeSpan.FromTicks(elapsedTicks);
    }

    private static readonly double s_stopWatchTicksToTimeSpanTicksMultiplier = 1e+7 / System.Diagnostics.Stopwatch.Frequency;
    internal static readonly double TicksToNsMultiplier = 1e+9 / System.Diagnostics.Stopwatch.Frequency;
}