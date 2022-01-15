using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using ModTek.Features.Logging;
using ModTek.Misc;
using ModTek.Util;

namespace ModTek.Features.Profiling
{
    internal class ModTekProfiler
    {
        private readonly ReaderWriterLockSlim timingsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private readonly Dictionary<object, TickCounter> timings = new Dictionary<object, TickCounter>();
        private readonly TickCounter minimumOverheadCounter = new TickCounter();
        private readonly TickCounter stackTraceOverheadCounter = new TickCounter();
        private readonly TickCounter dumpOverheadCounter = new TickCounter();
        private readonly ModTekProfilerSettings settings = ModTek.Config.Profiling.ModTekProfiler;

        internal void IncrementWrapper(object target, TickTracker tracker, bool allowStackTrace = true, bool trackTime = true)
        {
            try
            {
                Increment(target, tracker, allowStackTrace, trackTime);
            }
            catch (Exception e)
            {
                MTLogger.Error.Log("Failed running postfix", e);
            }
        }

        private void Increment(object target, TickTracker tracker, bool allowStackTrace = true, bool trackTime = true)
        {
            var overheadTracker = new TickTracker();
            if (trackTime)
            {
                overheadTracker.Begin();
            }

            if (settings.StackTraceEnabled && allowStackTrace)
            {
                var stackTraceOverheadTracker = new TickTracker();
                stackTraceOverheadTracker.Begin();
                var st = new StackTrace(3, false);
                var maxFrames = settings.StackTraceMaxFrameCount;
                for(var frameIndex=0; frameIndex<st.FrameCount; frameIndex++)
                {
                    var sf = st.GetFrame(frameIndex);
                    if (sf == null)
                    {
                        continue;
                    }
                    var m = sf.GetMethod();
                    if (m == null)
                    {
                        continue;
                    }

                    Increment(new MethodVia(m, target), tracker, false, false);
                    if (--maxFrames == 0)
                    {
                        break;
                    }
                }
                stackTraceOverheadTracker.End();
                stackTraceOverheadCounter.IncrementBy(stackTraceOverheadTracker);
            }

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

            counter.IncrementBy(tracker);

            if (trackTime)
            {
                overheadTracker.End();
                minimumOverheadCounter.IncrementBy(overheadTracker);
            }
        }

        internal void DumpAndResetIfSlowWrapper(float frameDeltaSeconds)
        {
            try
            {
                DumpAndResetIfSlow(frameDeltaSeconds);
            }
            catch (Exception e)
            {
                MTLogger.Error.Log("Failed running prefix", e);
            }
        }

        private void DumpAndResetIfSlow(float frameDeltaSeconds)
        {
            var overheadTracker = new TickTracker();
            overheadTracker.Begin();

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

            if (frameDeltaSeconds > ModTek.Config.Profiling.ModTekProfiler.DumpWhenFrameTimeDeltaLargerThan)
            {
                var list = timingsCopy
                    .Select(kv => new Snapshot(kv.Key, kv.Value))
                    .ToList();

                list.Add(new Snapshot("Profiler Overhead (overall)", minimumOverheadCounter));
                list.Add(new Snapshot("Profiler Overhead (stacktrace)", stackTraceOverheadCounter));
                list.Add(new Snapshot("Profiler Overhead (dump)", dumpOverheadCounter));

                var frameDelta = TimeSpan.FromTicks((long)((double)frameDeltaSeconds*TimeSpan.TicksPerSecond));
                list.Add(new Snapshot("Last Frame Delta Time", frameDelta));

                var selected = list
                    .Where(kv => kv.Delta.TotalMilliseconds >= 1)
                    .OrderByDescending(kv => kv.Delta)
                    .Take(20)
                    .ToList();

                MTLogger.Info.Log($"dumping profiler stats, last frame above threshold ({frameDeltaSeconds})");

                {
                    var dump = $"\tdelta since last frame:";
                    foreach (var kv in selected)
                    {
                        var id = GetIdFromObject(kv.Target);
                        var p = kv.Delta.Ticks / (float)frameDelta.Ticks;
                        dump += $"\nd {kv.Delta:c} {p:P0} {id}";
                    }
                    MTLogger.Info.Log(dump);
                }

                {
                    var dump = "\ttotal times in the order of the deltas:";
                    foreach (var kv in selected)
                    {
                        var id = GetIdFromObject(kv.Target);
                        dump += $"\nt {kv.Total:c} ({kv.Count}) {id}";
                    }
                    MTLogger.Info.Log(dump);
                }

                {
                    var path = FilePaths.ProfilingSummaryPath;
                    MTLogger.Info.Log($"Writing all totals to {path}");
                    var top = list.Where(kv => kv.Total.TotalMilliseconds >= 100).OrderByDescending(kv => kv.Total);
                    using (var writer = File.CreateText(path))
                    {
                        foreach (var kv in top)
                        {
                            var id = GetIdFromObject(kv.Target);
                            writer.WriteLine($"{kv.Total:c} ({kv.Count}) {id}");
                        }
                    }
                }
            }
            else
            {
                foreach (var kv in timingsCopy)
                {
                    kv.Value.Reset();
                }
                minimumOverheadCounter.Reset();
                stackTraceOverheadCounter.Reset();
                dumpOverheadCounter.Reset();
            }

            overheadTracker.End();
            dumpOverheadCounter.IncrementBy(overheadTracker);
        }

        internal class Snapshot
        {
            internal readonly object Target;
            internal readonly TimeSpan Delta;
            internal readonly TimeSpan Total;
            internal readonly long Count;

            internal Snapshot(object target, TickCounter counter)
            {
                Target = target;
                Delta = counter.GetTotalSinceResetAndReset();
                Total = counter.GetTotal();
                Count = counter.GetCount();
            }

            internal Snapshot(object target, TimeSpan span)
            {
                Target = target;
                Delta = span;
                Total = span;
                Count = 1;
            }
        }

        internal static string GetIdFromObject(object o)
        {
            if (o is Type t)
            {
                return t.FullName;
            }

            if (o is MethodBase b)
            {
                return b.GetFullName();
            }

            return o.ToString();
        }
    }
}
