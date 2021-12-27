using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using ModTek.Features.Logging;
using ModTek.Util;
using UnityEngine;

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

        internal void Increment(MethodBase __originalMethod, long deltaRawTicks)
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

            minimumOverheadCounter.IncrementBy(GetRawTicks() - overheadStart);
        }

        internal void DumpAndResetIfSlow()
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

            if (Time.deltaTime > ModTek.Config.Profiling.DumpWhenFrameTimeDeltaLargerThan)
            {
                var list = timingsCopy
                    .Select(kv => (Method: kv.Key, Delta: kv.Value.GetAndReset(), Total: kv.Value.GetTotal()))
                    .Where(kv => kv.Delta.TotalMilliseconds >= 1)
                    .ToList();

                MTLogger.Log($"dumping profiler stats, last frame was slow ({Time.deltaTime})");
                {
                    var dump = "\tdelta since last frame ";
                    dump += $"\n{minimumOverheadCounter.Get():c} Profiler Overhead (minimum)";
                    foreach (var kv in list.OrderByDescending(kv => kv.Delta))
                    {
                        var id = AssemblyUtil.GetMethodFullName(kv.Method);
                        dump += $"\n{kv.Delta:c} {id}";
                    }
                    MTLogger.Log(dump);
                }

                {
                    var dump = "\ttotal times of methods listed before";
                    dump += $"\n{minimumOverheadCounter.GetTotal():c} Profiler Overhead (minimum)";
                    foreach (var kv in list.OrderByDescending(kv => kv.Total))
                    {
                        var id = AssemblyUtil.GetMethodFullName(kv.Method);
                        dump += $"\n{kv.Total:c} {id}";
                    }
                    MTLogger.Log(dump);
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
    }
}
