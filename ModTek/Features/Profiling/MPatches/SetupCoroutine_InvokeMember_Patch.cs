using System.Reflection;
using Harmony;

namespace ModTek.Features.Profiling.MPatches
{
    internal static class SetupCoroutine_InvokeMember_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("UnityEngine.SetupCoroutine:InvokeMember");
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(out TickTracker __state)
        {
            __state = new TickTracker();
            __state.Begin();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(object behaviour, string name, TickTracker __state)
        {
            __state.End();
            ProfilerPatcher.ModTekProfiler.IncrementWrapper(behaviour.GetType().GetMethod(name), __state, false);
        }
    }
}
