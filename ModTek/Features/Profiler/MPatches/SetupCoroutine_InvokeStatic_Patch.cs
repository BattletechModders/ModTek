using System;
using System.Reflection;
using Harmony;
using ModTek.Features.Logging;

namespace ModTek.Features.Profiler.MPatches
{
    internal static class SetupCoroutine_InvokeStatic_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("UnityEngine.SetupCoroutine:InvokeStatic");
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(out long __state)
        {
            __state = ProfilerPatcher.timings.GetRawTicks();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(Type klass, string name, long __state)
        {
            try
            {
                var deltaRawTicks = ProfilerPatcher.timings.GetRawTicks() - __state;
                ProfilerPatcher.timings.Increment(klass.GetMethod(name), deltaRawTicks);
            }
            catch (Exception e)
            {
                MTLogger.Log("Error running postfix", e);
            }
        }
    }
}
