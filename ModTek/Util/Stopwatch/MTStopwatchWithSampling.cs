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
    }
    internal readonly uint _samplingInterval;
    private readonly ulong _sampleIfRandomSmallerOrEqualsTo;
    private readonly FastRandom _random = new();

    internal static readonly double SamplingCheckOverhead; // 1.554ns
    internal static readonly bool DontOptimize;
    static MTStopwatchWithSampling()
    {
        var ws = new MTStopwatchWithSampling(100);
        const int Count = 100_000;
        const int WarmupCount = Count/2;
        const double ActualCount = Count - WarmupCount;
        var sum = 0L;
        var dontOptimize = false;
        for (var i = 0; i < Count; i++)
        {
            var start = GetTimestamp();
            dontOptimize = ws.ShouldMeasure();
            var end = GetTimestamp();
            if (i >= WarmupCount)
            {
                sum += end - start;
            }
        }
        SamplingCheckOverhead = (sum - GetTimestampOverheadInMeasurement * ActualCount) / ActualCount;
        DontOptimize = dontOptimize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ShouldMeasure()
    {
        return _random.NextUInt64() <= _sampleIfRandomSmallerOrEqualsTo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal override void EndMeasurement(long start)
    {
        if (ShouldMeasure())
        {
            AddMeasurement((GetTimestamp() - start) * _samplingInterval, _samplingInterval);
        }
    }
}