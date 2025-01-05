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
    protected override void AddMeasurement(long elapsedTicks, long delta)
    {
        var count = Interlocked.Add(ref _count, delta);
        var ticks = Interlocked.Add(ref _ticks, elapsedTicks);
        if ((count & CallbackFastModuloMask) == 0)
        {
            _callback.Invoke(new MTStopwatchStats(this, count, ticks));
        }
    }
    private const long CallbackEveryMeasurement = 1 << 14; // every 16k, FastModulo requires base 2
    private const long CallbackFastModuloMask = CallbackEveryMeasurement - 1;
}