using System.Reflection;
using BattleTech;
using Harmony;
using UnityEngine;

namespace ModTek.Features.Profiling.MPatches
{
    internal static class UnityGameInstance_Update_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(UnityGameInstance), "Update");
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(out TickTracker __state)
        {
            ProfilerPatcher.ModTekProfiler.DumpAndResetIfSlowWrapper(Time.deltaTime);
            __state = new TickTracker();
            __state.Begin();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(MethodBase __originalMethod, TickTracker __state)
        {
            __state.End();
            ProfilerPatcher.ModTekProfiler.IncrementWrapper(__originalMethod, __state);
        }
    }
}
