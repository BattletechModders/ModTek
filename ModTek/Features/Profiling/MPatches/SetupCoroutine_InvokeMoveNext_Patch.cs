using System.Collections;
using System.Reflection;
using Harmony;

namespace ModTek.Features.Profiling.MPatches
{
    internal static class SetupCoroutine_InvokeMoveNext_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("UnityEngine.SetupCoroutine:InvokeMoveNext");
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(out TickTracker __state)
        {
            __state = new TickTracker();
            __state.Begin();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(IEnumerator enumerator, TickTracker __state)
        {
            __state.End();
            ProfilerPatcher.ModTekProfiler.IncrementWrapper(enumerator.GetType(), __state, false);
        }
    }
}
