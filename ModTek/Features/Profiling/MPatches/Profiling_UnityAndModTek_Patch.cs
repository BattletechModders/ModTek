#define ENABLE_PROFILER

using System.Reflection;
using ModTek.Util;
using UnityEngine.Profiling;

namespace ModTek.Features.Profiling.MPatches
{
    internal static class Profiling_UnityAndModTek_Patch
    {
        internal static void Prefix(MethodBase __originalMethod, out TrackerSampler __state)
        {
            __state = new TrackerSampler(__originalMethod);
            __state.Begin();
        }

        internal static void Postfix(MethodBase __originalMethod, TrackerSampler __state)
        {
            __state.End();
            ProfilerPatcher.ModTekProfiler.IncrementWrapper(__originalMethod, __state.tracker);
        }
    }

    internal class TrackerSampler
    {
        internal TickTracker tracker;
        internal CustomSampler sampler;

        internal TrackerSampler(MemberInfo originalMethod)
        {
            tracker = new TickTracker();
            sampler = CustomSampler.Create(originalMethod.GetFullName());
        }

        internal void Begin()
        {
            sampler.Begin();
            tracker.Begin();
        }

        internal void End()
        {
            tracker.End();
            sampler.End();
        }
    }
}
