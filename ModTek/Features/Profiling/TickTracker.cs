using System.Diagnostics;

namespace ModTek.Features.Profiling
{
    internal struct TickTracker
    {
        internal long RawTicks;

        internal void Begin()
        {
            RawTicks = GetRawTicks();
        }

        internal void End()
        {
            RawTicks = GetRawTicks() - RawTicks;
        }

        private static long GetRawTicks()
        {
            return Stopwatch.GetTimestamp();
        }
    }
}
