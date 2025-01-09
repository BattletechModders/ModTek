using System.Runtime.CompilerServices;

namespace ModTek.Util.Stopwatch;

// Stopwatch.GetTimestamp takes about 16-30ns, probably due to "extern" overhead
// fast random is much faster, runs unrolled and therefore in parallel on the CPU
internal sealed class MTStopwatchWithSampling : MTStopwatch
{
    internal MTStopwatchWithSampling(uint samplingInterval)
    {
        _samplingInterval = samplingInterval;
        _sampleIfRandomSmallerOrEqualsTo = ulong.MaxValue / samplingInterval;
        _overheadInMeasurement = _overheadInMeasurement / _samplingInterval + s_samplingCheckOverhead;
    }
    internal readonly uint _samplingInterval;
    private readonly ulong _sampleIfRandomSmallerOrEqualsTo;
    private readonly FastRandom _random = new();

    internal double OverheadPerMeasurementWithSampling => OverheadPerMeasurement/_samplingInterval + s_samplingCheckOverhead;
    internal double OverheadPerMeasurementWithoutSampling => OverheadPerMeasurement;

    private static readonly double s_samplingCheckOverhead;
    internal static readonly bool DontOptimize;
    static MTStopwatchWithSampling()
    {
        var ws = new MTStopwatchWithSampling(100);
        var dontOptimize = false;
        for (var r = 0; r < 100; r++)
        {
            var start = GetTimestamp();
            const int Count = 1000;
            for (var i = 0; i < Count; i++)
            {
                dontOptimize = ws.ShouldMeasure();
            }
            var end = GetTimestamp();
            s_samplingCheckOverhead = (end - start)/(double)Count - s_timestampOverhead;
            DontOptimize = dontOptimize;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ShouldMeasure()
    {
        return _random.NextUInt64() <= _sampleIfRandomSmallerOrEqualsTo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal override void EndMeasurement(long start, long delta = 1)
    {
        if (ShouldMeasure())
        {
            AddMeasurement((GetTimestamp() - start) * _samplingInterval, delta * _samplingInterval);
        }
    }
}