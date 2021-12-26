using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using ModTek.Features.Logging;
using ModTek.Util;

namespace ModTek.Features.Profiler
{
    internal class Timings
    {
        private readonly ReaderWriterLockSlim timingsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly Dictionary<MethodBase, TickCounter> timings = new Dictionary<MethodBase, TickCounter>();
        private readonly TickCounter minimumOverheadCounter = new TickCounter();

        internal long GetRawTicks()
        {
            return Stopwatch.GetTimestamp();
        }

        internal void Increment(MethodBase __originalMethod, long deltaRawTicks, bool dumpCheck)
        {
            var overheadStart = GetRawTicks();

            TickCounter counter;
            timingsLock.EnterUpgradeableReadLock();
            try
            {
                if (!timings.TryGetValue(__originalMethod, out counter))
                {
                    timingsLock.EnterWriteLock();
                    try
                    {
                        // since we only now have an exclusive lock
                        // we need to check if someone else added a counter before us
                        if (!timings.TryGetValue(__originalMethod, out counter))
                        {
                            counter = new TickCounter();
                            timings[__originalMethod] = counter;
                        }
                    }
                    finally
                    {
                        timingsLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                timingsLock.ExitUpgradeableReadLock();
            }

            counter.IncrementBy(deltaRawTicks);

            if (dumpCheck)
            {
                // we copy inside a lock in order to safely iterate
                List<KeyValuePair<MethodBase, TickCounter>> timingsCopy;
                timingsLock.EnterReadLock();
                try
                {
                    timingsCopy = timings.ToList();
                }
                finally
                {
                    timingsLock.ExitReadLock();
                }

                const long DumpWhenFrameTimeSlowerThanMS = 1000L / 30; // TODO config
                if (counter.Get().TotalMilliseconds > DumpWhenFrameTimeSlowerThanMS)
                {

                    var list = timingsCopy
                        .Select(kv => (Method: kv.Key, Delta: kv.Value.GetAndReset(), Total: kv.Value.GetTotal()))
                        .Where(kv => kv.Delta.TotalMilliseconds >= 1)
                        .ToList();

                    MTLogger.Log("Too slow, dumping profiler stats, delta since last frame");
                    MTLogger.Log($"\t{minimumOverheadCounter.Get():c} Minimum Profiler Overhead");
                    foreach (var kv in list.OrderByDescending(kv => kv.Delta))
                    {
                        var id = kv.Method.DeclaringType?.FullName + "." + kv.Method.Name;
                        MTLogger.Log($"\t{kv.Delta:c} {id}");
                    }

                    const bool ShowTotalsForShownDeltaEntriesFromBefore = true;
                    if (ShowTotalsForShownDeltaEntriesFromBefore)
                    {
                        MTLogger.Log("Similar profiler dump as before, but with total time");
                        MTLogger.Log($"\t{minimumOverheadCounter.GetTotal():c} Minimum Profiler Overhead");
                        foreach (var kv in list.OrderByDescending(kv => kv.Total))
                        {
                            var id = AssemblyUtil.GetMethodFullName(kv.Method);
                            MTLogger.Log($"\t{kv.Total:c} {id}");
                        }
                    }
                }
                else
                {
                    foreach (var kv in timingsCopy)
                    {
                        kv.Value.Reset();
                    }
                }
                minimumOverheadCounter.Reset();
            }

            minimumOverheadCounter.IncrementBy(GetRawTicks() - overheadStart);
        }
    }
}
