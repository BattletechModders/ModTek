using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ModTek.Util.Stopwatch;

internal class MTStopwatch
{
    protected long _count;
    protected long _ticks;

    internal double _overheadInMeasurement = s_timestampOverheadInMeasurement;

    internal byte TimestampCountPerMeasurement { get; init; } = 2;
    protected double OverheadPerMeasurement => s_timestampOverheadInAndAfterMeasurement * TimestampCountPerMeasurement;
    internal static double OverheadPerTimestampInNanoseconds => s_timestampOverheadInAndAfterMeasurement * TicksToNsMultiplier;

    protected static readonly double s_timestampOverheadInMeasurement; // 22.47ns
    private static readonly double s_timestampOverheadInAndAfterMeasurement; // 23.216ns
    static MTStopwatch()
    {
        const int Count = 100_000;
        const int WarmupCount = Count/2;
        const double ActualCount = Count - WarmupCount;
        var smSum = 0L;
        var seSum = 0L;
        for (var i = 0; i < Count; i++)
        {
            var start = GetTimestamp();
            // no operation in here, so mid-start should contain captured measurement overhead
            var mid = GetTimestamp();
            // we still want the actual total overhead too, so lets measure after the mid again
            var end = GetTimestamp();
            if (i >= WarmupCount)
            {
                smSum += mid - start;
                seSum += end - start;
            }
        }
        s_timestampOverheadInMeasurement = smSum / ActualCount;
        s_timestampOverheadInAndAfterMeasurement = ( seSum - smSum ) / ActualCount;
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

    internal static long FastestTicksSum(long[] ticks, double onlyIncludeFastest = 0.5)
    {
        Array.Sort(ticks);
        var sum = 0L;
        for (var i = 0; i < ticks.Length * onlyIncludeFastest; i++)
        {
            sum += ticks[i];
        }
        return sum;
    }

    internal static TimeSpan TimeSpanFromTicks(long elapsedTicks)
    {
        return System.Diagnostics.Stopwatch.IsHighResolution ? TimeSpan.FromTicks((long)(elapsedTicks * s_stopWatchTicksToTimeSpanTicksMultiplier)) : TimeSpan.FromTicks(elapsedTicks);
    }

    private static readonly double s_stopWatchTicksToTimeSpanTicksMultiplier = 1e+7 / System.Diagnostics.Stopwatch.Frequency;
    internal static readonly double TicksToNsMultiplier = 1e+9 / System.Diagnostics.Stopwatch.Frequency;
}