#define ENABLE_PROFILER

using System.Reflection;
using ModTek.Util;
using UnityEngine.Profiling;

namespace ModTek.Features.Profiling.MPatches
{
    internal static class Profiling_UnityOnly_Patch
    {
        internal static void Prefix(MethodBase __originalMethod, out CustomSampler __state)
        {
            __state = CustomSampler.Create(__originalMethod.GetFullName());
            __state.Begin();
        }

        internal static void Postfix(MethodBase __originalMethod, CustomSampler __state)
        {
            __state.End();
        }
    }
}
