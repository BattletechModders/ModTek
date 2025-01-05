using System;

namespace ModTek.Util.Stopwatch;

internal readonly struct MTStopwatchStats
{
    private readonly string _verb;
    internal readonly long Count;
    internal readonly long Ticks;
    internal TimeSpan TotalTime => MTStopwatch.TimeSpanFromTicks(Ticks);
    internal long AverageNanoseconds => Count <= 0 ? 0 : (long)((double)Ticks / Count / System.Diagnostics.Stopwatch.Frequency * 1e+9);

    internal MTStopwatchStats(MTStopwatch sw, long count, long ticks)
    {
        Count = count;
        Ticks = ticks;
        _verb = sw is MTStopwatchWithSampling ? "estimated at" : "measured";
    }

    public override string ToString()
    {
        return $"{_verb} {Count} times, taking a total of {TotalTime} with an average of {AverageNanoseconds}ns";
    }
}