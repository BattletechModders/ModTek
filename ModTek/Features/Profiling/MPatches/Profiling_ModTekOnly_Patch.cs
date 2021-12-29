using System.Reflection;

namespace ModTek.Features.Profiling.MPatches
{
    internal static class Profiling_ModTekOnly_Patch
    {
        internal static void Prefix(MethodBase __originalMethod, out TickTracker __state)
        {
            __state = new TickTracker();
            __state.Begin();
        }

        internal static void Postfix(MethodBase __originalMethod, TickTracker __state)
        {
            __state.End();
            ProfilerPatcher.ModTekProfiler.IncrementWrapper(__originalMethod, __state);
        }
    }
}
