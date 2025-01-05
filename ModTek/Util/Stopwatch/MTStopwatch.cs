using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ModTek.Util.Stopwatch;

internal class MTStopwatch
{
    protected long _count;
    protected long _ticks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTimestamp()
    {
        return System.Diagnostics.Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal virtual void EndMeasurement(long start)
    {
        AddMeasurement(GetTimestamp() - start, 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void AddMeasurement(long elapsedTicks, long delta)
    {
        Interlocked.Add(ref _count, delta);
        Interlocked.Add(ref _ticks, elapsedTicks);
    }

    internal MTStopwatchStats GetStats() => new(this, Volatile.Read(ref _count), Volatile.Read(ref _ticks));

    internal static TimeSpan TimeSpanFromTicks(long elapsedTicks)
    {
        return System.Diagnostics.Stopwatch.IsHighResolution ? TimeSpan.FromTicks(elapsedTicks / (System.Diagnostics.Stopwatch.Frequency / 10000000L)) : TimeSpan.FromTicks(elapsedTicks);
    }
}