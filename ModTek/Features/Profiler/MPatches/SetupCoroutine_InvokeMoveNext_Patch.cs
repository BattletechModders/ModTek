using System;
using System.Collections;
using System.Reflection;
using Harmony;
using ModTek.Features.Logging;

namespace ModTek.Features.Profiler.MPatches
{
    internal static class SetupCoroutine_InvokeMoveNext_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("UnityEngine.SetupCoroutine:InvokeMoveNext");
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(out long __state)
        {
            __state = ProfilerPatcher.timings.GetRawTicks();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(IEnumerator enumerator, long __state)
        {
            try
            {
                var deltaRawTicks = ProfilerPatcher.timings.GetRawTicks() - __state;
                ProfilerPatcher.timings.Increment(enumerator.GetType(), deltaRawTicks);
            }
            catch (Exception e)
            {
                MTLogger.Log("Error running postfix", e);
            }
        }
    }
}
