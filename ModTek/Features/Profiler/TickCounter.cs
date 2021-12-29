using System;
using System.Diagnostics;
using System.Threading;

namespace ModTek.Features.Profiler
{
    // simple wrapper around a long counter
    // uses Interlocked to have atomic semantics
    // this class is Thread safe
    internal class TickCounter
    {
        private long totalRawTicks;
        private long totalRawTicksSinceReset;
        private long incrementCount;

        internal void IncrementBy(long deltaRawTicks)
        {
            Interlocked.Add(ref totalRawTicks, deltaRawTicks);
            Interlocked.Add(ref totalRawTicksSinceReset, deltaRawTicks);
            Interlocked.Add(ref incrementCount, 1);
        }

        internal TimeSpan GetTotal()
        {
            var snapshot = Interlocked.Read(ref totalRawTicks);
            return ConvertRawTicksToTimeSpan(snapshot);
        }

        internal long GetCount()
        {
            return Interlocked.Read(ref incrementCount);
        }

        internal void Reset()
        {
            Interlocked.Exchange(ref totalRawTicksSinceReset, 0);
        }

        internal TimeSpan GetAndReset()
        {
            var snapshot = Interlocked.Exchange(ref totalRawTicksSinceReset, 0);
            return ConvertRawTicksToTimeSpan(snapshot);
        }

        internal TimeSpan Get()
        {
            var snapshot = Interlocked.Read(ref totalRawTicksSinceReset);
            return ConvertRawTicksToTimeSpan(snapshot);
        }

        private static TimeSpan ConvertRawTicksToTimeSpan(long rawTicks)
        {
            var dateTimeTicks = ConvertRawTicksToDateTimeTicks(rawTicks);
            return new TimeSpan(dateTimeTicks);
        }

        private static long ConvertRawTicksToDateTimeTicks(long rawTicks)
        {
            if (Stopwatch.IsHighResolution)
            {
                // copied from TimeSpan, as private and not accessible
                var tickFrequency = 10000000.0;
                tickFrequency /= Stopwatch.Frequency;
                return (long)(rawTicks * tickFrequency);
            }
            return rawTicks;
        }
    }
}
