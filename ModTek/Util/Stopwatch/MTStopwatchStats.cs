using System;

namespace ModTek.Util.Stopwatch;

internal readonly struct MTStopwatchStats
{
    private readonly long _samplingInterval;
    internal readonly long Count;
    internal readonly long Ticks;
    internal TimeSpan TotalTime => MTStopwatch.TimeSpanFromTicks(Ticks);
    internal long AverageNanoseconds => Count <= 0 ? 0 : (long)(MTStopwatch.TicksToNsMultiplier * Ticks / Count);

    internal MTStopwatchStats(MTStopwatch sw, long count, long ticks)
    {
        Count = count;
        var overheadInMeasurement = MTStopwatch.GetTimestampOverheadInMeasurement;
        if (sw is MTStopwatchWithSampling sws)
        {
            _samplingInterval = sws._samplingInterval;
            overheadInMeasurement += MTStopwatchWithSampling.SamplingCheckOverhead;
        }
        Ticks = ticks - (long)(overheadInMeasurement * count);
    }

    public override string ToString()
    {
        var verb = "measured";
        var suffix = "";
        if (_samplingInterval > 1)
        {
            verb = "estimated at";
            var sampled = Count / _samplingInterval;
            var ifAllMeasured = Count * MTStopwatch.GetTimestampOverheadInAndAfterMeasurement;
            var onlySampledMeasured = sampled * MTStopwatch.GetTimestampOverheadInAndAfterMeasurement + Count * MTStopwatchWithSampling.SamplingCheckOverhead;
            var saved = ifAllMeasured - onlySampledMeasured;
            var savedTimeSan = MTStopwatch.TimeSpanFromTicks((long)saved);
            suffix = $", sampling interval of {_samplingInterval} saved {savedTimeSan}";
        }
        return $"{verb} {Count} times, taking a total of {TotalTime} with an average of {AverageNanoseconds}ns{suffix}";
    }
}