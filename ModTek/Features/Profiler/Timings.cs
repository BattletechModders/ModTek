using System;
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
        private readonly Dictionary<object, TickCounter> timings = new Dictionary<object, TickCounter>();
        private readonly TickCounter minimumOverheadCounter = new TickCounter();

        internal long GetRawTicks()
        {
            return Stopwatch.GetTimestamp();
        }

        internal void Increment(object target, long deltaRawTicks)
        {
            var overheadStart = GetRawTicks();

            TickCounter counter;
            timingsLock.EnterUpgradeableReadLock();
            try
            {
                if (!timings.TryGetValue(target, out counter))
                {
                    timingsLock.EnterWriteLock();
                    try
                    {
                        // since we only now have an exclusive lock
                        // we need to check if someone else added a counter before us
                        if (!timings.TryGetValue(target, out counter))
                        {
                            counter = new TickCounter();
                            timings[target] = counter;
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

        internal void DumpAndResetIfSlow(float deltaTime)
        {
            // we copy inside a lock in order to safely iterate
            List<KeyValuePair<object, TickCounter>> timingsCopy;
            timingsLock.EnterReadLock();
            try
            {
                timingsCopy = timings.ToList();
            }
            finally
            {
                timingsLock.ExitReadLock();
            }

            if (deltaTime > ModTek.Config.Profiling.DumpWhenFrameTimeDeltaLargerThan)
            {
                var list = timingsCopy
                    .Select(kv => (Target: kv.Key, Delta: kv.Value.GetAndReset(), Total: kv.Value.GetTotal()))
                    .Where(kv => kv.Delta.TotalMilliseconds >= 1)
                    .ToList();

                MTLogger.Log($"dumping profiler stats, last frame was slow ({Time.deltaTime})");
                {
                    var dump = "\tdelta since last frame ";
                    dump += $"\n{minimumOverheadCounter.Get():c} Profiler Overhead (minimum)";
                    foreach (var kv in list.OrderByDescending(kv => kv.Delta))
                    {
                        var id = GetIdFromObject(kv.Target);
                        dump += $"\n{kv.Delta:c} {id}";
                    }
                    MTLogger.Log(dump);
                }

                {
                    var dump = "\ttotal times listed before";
                    dump += $"\n{minimumOverheadCounter.GetTotal():c} Profiler Overhead (minimum)";
                    foreach (var kv in list.OrderByDescending(kv => kv.Total))
                    {
                        var id = GetIdFromObject(kv.Target);
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

        private static string GetIdFromObject(object o)
        {
            if (o is Type t)
            {
                return t.FullName;
            }

            if (o is MethodBase b)
            {
                return AssemblyUtil.GetMethodFullName(b);
            }

            return o.ToString();
        }
    }
}
