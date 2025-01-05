using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ModTek.Util.Stopwatch;

internal class MTStopwatch
{
    protected long _ticks;
    protected long _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long GetTimestamp()
    {
        return System.Diagnostics.Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal virtual void EndMeasurement(long start)
    {
        AddMeasurement(GetTimestamp() - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void AddMeasurement(long elapsedTicks)
    {
        Interlocked.Increment(ref _count);
        Interlocked.Add(ref _ticks, elapsedTicks);
    }

    internal MTStopwatchStats GetStats() => new(this, Interlocked.Read(ref _ticks), Interlocked.Read(ref _count));

    internal static TimeSpan TimeSpanFromTicks(long elapsedTicks)
    {
        return System.Diagnostics.Stopwatch.IsHighResolution ? TimeSpan.FromTicks(elapsedTicks / (System.Diagnostics.Stopwatch.Frequency / 10000000L)) : TimeSpan.FromTicks(elapsedTicks);
    }
}