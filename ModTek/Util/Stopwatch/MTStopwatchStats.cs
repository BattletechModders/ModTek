using System;

namespace ModTek.Util.Stopwatch;

internal readonly struct MTStopwatchStats
{
    private readonly string _verb;
    internal readonly long Ticks;
    internal readonly long Count;
    internal TimeSpan TotalTime => MTStopwatch.TimeSpanFromTicks(Ticks);
    internal long AverageNanoseconds => Count <= 0 ? 0 : (long)((double)Ticks / Count / System.Diagnostics.Stopwatch.Frequency * 1e+9);

    internal MTStopwatchStats(MTStopwatch sw, long ticks, long count)
    {
        if (sw is MTStopwatchWithSampling swWithSampling)
        {
            Ticks = ticks * swWithSampling.Sampling;
            Count = count * swWithSampling.Sampling;
            _verb = "estimated at";
        }
        else
        {
            Ticks = ticks;
            Count = count;
            _verb = "measured";
        }
    }

    public override string ToString()
    {
        return $"{_verb} {Count} times, taking a total of {TotalTime} with an average of {AverageNanoseconds}ns";
    }
}