using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ModTek.Util.Stopwatch;

internal sealed class MTStopwatchWithCallback : MTStopwatch
{
    internal MTStopwatchWithCallback(Action<MTStopwatchStats> callback)
    {
        this._callback = callback;
    }
    private readonly Action<MTStopwatchStats> _callback;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void AddMeasurement(long elapsedTicks)
    {
        var count = Interlocked.Increment(ref _count);
        var ticks = Interlocked.Add(ref _ticks, elapsedTicks);
        if ((count & CallbackFastModuloMask) == 0)
        {
            _callback.Invoke(new MTStopwatchStats(this, ticks, count));
        }
    }
    private const long CallbackEveryMeasurement = 1 << 14; // every 16k, FastModulo requires base 2
    private const long CallbackFastModuloMask = CallbackEveryMeasurement - 1;
}