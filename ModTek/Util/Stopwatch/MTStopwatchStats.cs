using System;

namespace ModTek.Util.Stopwatch;

internal readonly struct MTStopwatchStats
{
    private readonly MTStopwatch _sw;
    internal readonly long Count;
    internal readonly long Ticks;
    internal TimeSpan TotalTime => MTStopwatch.TimeSpanFromTicks(Ticks);
    internal long AverageNanoseconds => Count <= 0 ? 0 : (long)(MTStopwatch.TicksToNsMultiplier * Ticks / Count);

    internal MTStopwatchStats(MTStopwatch sw, long count, long ticks)
    {
        _sw = sw;
        Count = count;
        Ticks = ticks - (long)(sw._overheadInMeasurement * count);
    }

    public override string ToString()
    {
        var verb = "measured";
        var suffix = "";
        if (_sw is MTStopwatchWithSampling sws)
        {
            verb = "estimated at";
            var overheadWithoutSampling = sws.OverheadPerMeasurementWithoutSampling * Count;
            var saved = overheadWithoutSampling - sws.OverheadPerMeasurementWithSampling * Count;
            var savedPercent = (byte)(saved / (Ticks + overheadWithoutSampling) * 100);
            suffix = $", sampling interval of {sws._samplingInterval} reduced measurement overhead by {savedPercent}%";
        }
        return $"{verb} {Count} times, taking a total of {TotalTime} with an average of {AverageNanoseconds}ns{suffix}";
    }
}