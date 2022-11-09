using System;
using System.Threading;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace ModTek.Features.Logging
{
    internal class MTStopwatch
    {
        internal Action<Stats> Callback { get; set; }
        internal long CallbackForEveryNumberOfMeasurements { get; set; } = 100;

        private long _ticks;
        private long _count;
        private Tracker _actionTracker;

        internal void Track(Action action)
        {
            _actionTracker.Begin();
            try
            {
                action();
            }
            finally
            {
                var elapsed = _actionTracker.End();
                AddMeasurement(elapsed);
            }
        }

        internal void AddMeasurement(long elapsedTicks, long incrementCount = 1)
        {
            var ticks = Interlocked.Add(ref _ticks, elapsedTicks);
            if (Callback != null && incrementCount != 0)
            {
                var count = Interlocked.Add(ref _count, incrementCount);
                if (count % CallbackForEveryNumberOfMeasurements == 0)
                {
                    Callback.Invoke(new Stats(ticks, count));
                }
            }
        }

        internal struct Tracker
        {
            private long _begin;
            internal void Begin()
            {
                _begin = Stopwatch.GetTimestamp();
            }

            internal long End()
            {
                return Stopwatch.GetTimestamp() - _begin;
            }
        }

        internal Stats GetStats() => new Stats(this);
        internal readonly struct Stats
        {
            internal long Ticks { get; }
            internal long Count { get; }
            internal long TotalMS => TicksToMS(Ticks);

            internal Stats(MTStopwatch sw)
            {
                Ticks = Interlocked.Read(ref sw._ticks);
                Count = Interlocked.Read(ref sw._count);
            }

            internal Stats(long ticks, long count)
            {
                Ticks = ticks;
                Count = count;
            }

            private static long TicksToMS(long ticks)
            {
                return Stopwatch.IsHighResolution ? ticks / (Stopwatch.Frequency / 1000L) : ticks;
            }
        }
    }
}
