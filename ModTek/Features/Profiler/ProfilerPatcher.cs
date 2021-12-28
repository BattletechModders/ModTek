using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using ModTek.Features.Profiler.MPatches;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Profiler
{
    internal static class ProfilerPatcher
    {
        internal static HarmonyInstance harmony;
        internal static Timings timings;

        internal static void Patch()
        {
            if (!ModTek.Enabled || !ModTek.Config.Profiling.Enabled)
            {
                return;
            }

            timings = new Timings();
            harmony = HarmonyInstance.Create("ModTek.Profiler");
            new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeMoveNext_Patch), null).Patch();
            new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeMember_Patch), null).Patch();
            new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeStatic_Patch), null).Patch();

            {
                var methods = MethodMatcher_Patcher.TargetMethods();
                var prefix = new HarmonyMethod(AccessTools.Method(typeof(MethodMatcher_Patcher), nameof(MethodMatcher_Patcher.Prefix)));
                prefix.prioritiy = Priority.First;
                var postfix = new HarmonyMethod(AccessTools.Method(typeof(MethodMatcher_Patcher), nameof(MethodMatcher_Patcher.Postfix)));
                postfix.prioritiy = Priority.Last;
                foreach (var method in methods)
                {
                    var processor = new PatchProcessor(
                        harmony,
                        new List<MethodBase> { method },
                        prefix,
                        postfix
                    );
                    try
                    {
                        processor.Patch();
                    }
                    catch (Exception e)
                    {
                        Log("Warning: Failed applying profiler patch", e);
                    }
                }
            }
            harmony = null;
        }
    }
}