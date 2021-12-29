using System;
using System.Reflection;
using Harmony;

namespace ModTek.Features.Profiling.MPatches
{
    internal static class SetupCoroutine_InvokeStatic_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("UnityEngine.SetupCoroutine:InvokeStatic");
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(out TickTracker __state)
        {
            __state = new TickTracker();
            __state.Begin();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(Type klass, string name, TickTracker __state)
        {
            __state.End();
            ProfilerPatcher.ModTekProfiler.IncrementWrapper(klass.GetMethod(name), __state, false);
        }
    }
}
