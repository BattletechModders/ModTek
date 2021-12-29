using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using ModTek.Features.Profiler.MPatches;
using ModTek.UI;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Profiler
{
    internal static class ProfilerPatcher
    {
        internal static HarmonyInstance harmony;
        internal static Timings timings;

        internal static IEnumerator<ProgressReport> ProfilerSetupLoop()
        {
            if (!ModTek.Enabled || !ModTek.Config.Profiling.Enabled)
            {
                yield break;
            }

            var sliderText = "Patching methods for profiling";
            yield return new ProgressReport(1, sliderText, "Gathering methods to be profiled", true);

            timings = new Timings();
            harmony = HarmonyInstance.Create("ModTek.Profiler");
            {
                var methodTuples = MethodMatcher_Patcher.TargetMethods()
                    .Select(m => (Method: m, AssemblyName: m.ReflectedType?.Assembly.GetName().Name, TypeName: m.ReflectedType?.FullName))
                    .OrderBy(t => t.AssemblyName)
                    .ThenBy(t => t.TypeName)
                    .ThenBy(t => t.Method.Name)
                    .ToList();
                var prefix = new HarmonyMethod(AccessTools.Method(typeof(MethodMatcher_Patcher), nameof(MethodMatcher_Patcher.Prefix)));
                prefix.prioritiy = Priority.First;
                var postfix = new HarmonyMethod(AccessTools.Method(typeof(MethodMatcher_Patcher), nameof(MethodMatcher_Patcher.Postfix)));
                postfix.prioritiy = Priority.Last;

                var countCurrent = 0;
                var countMax = (float) methodTuples.Count;
                foreach (var t in methodTuples)
                {
                    yield return new ProgressReport(
                        countCurrent++/countMax,
                        sliderText,
                        $"{t.AssemblyName}\n{t.TypeName}\n{t.Method.Name}"
                    );
                    var processor = new PatchProcessor(
                        harmony,
                        new List<MethodBase> { t.Method },
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

            if (ModTek.Config.Profiling.CoverCoroutines)
            {
                new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeMoveNext_Patch), null).Patch();
                new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeMember_Patch), null).Patch();
                new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeStatic_Patch), null).Patch();
            }

            harmony = null;
        }
    }
}